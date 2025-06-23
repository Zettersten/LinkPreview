using System.Net;
using Microsoft.Extensions.Caching.Memory;

namespace LinkPreview;

public sealed class LinkPreviewUrlVerifier(HttpClient httpClient, IMemoryCache cache)
{
    /// <summary>
    /// Verifies the given absolute URL by performing a GET request.
    /// - Returns the final URL if the request is successful (follows redirects).
    /// - Throws LinkPreviewException if the response status is 400 or above.
    /// - Uses in-memory cache for efficiency.
    /// </summary>
    /// <param name="url">The absolute URL to verify.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The final URL after following redirects.</returns>
    /// <exception cref="LinkPreviewException">Thrown if the response status is 400 or above.</exception>
    public async Task<string> VerifyAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL must not be null or empty.", nameof(url));

        // Use cache to avoid redundant network calls
        if (cache.TryGetValue<string?>(url, out var cachedResult) && cachedResult is not null)
        {
            return cachedResult;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            // Only fetch headers to minimize bandwidth
            request.Headers.UserAgent.ParseAdd(
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_11_1) AppleWebKit/601.2.4 (KHTML, like Gecko) Version/9.0.1 Safari/601.2.4 facebookexternalhit/1.1 Facebot Twitterbot/1.0"
            );
            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken
            );

            // Follow redirect manually if needed
            if (IsRedirect(response.StatusCode))
            {
                var location =
                    response.Headers.Location
                    ?? throw new LinkPreviewException(
                        HttpStatusCode.BadRequest,
                        "Redirect response missing Location header."
                    );

                var finalUrl = location.IsAbsoluteUri
                    ? location.ToString()
                    : new Uri(new Uri(url), location).ToString();

                // Cache the redirect result for both original and final URL
                cache.Set(
                    url,
                    finalUrl,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                    }
                );

                cache.Set(
                    finalUrl,
                    finalUrl,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                    }
                );

                return finalUrl;
            }

            if ((int)response.StatusCode >= 400)
            {
                throw new LinkPreviewException(
                    response.StatusCode,
                    $"URL returned error status: {(int)response.StatusCode} ({response.StatusCode})"
                );
            }

            if (
                response.IsSuccessStatusCode
                && (
                    response.RequestMessage != null
                    && response.RequestMessage.RequestUri != null
                    && !url.Equals(
                        response.RequestMessage.RequestUri.ToString(),
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            )
            {
                var finalUrl = response.RequestMessage.RequestUri.ToString();

                // Cache the redirect result for both original and final URL
                cache.Set(
                    url,
                    finalUrl,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                    }
                );

                cache.Set(
                    finalUrl,
                    finalUrl,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                    }
                );

                return finalUrl;
            }

            // Success, cache and return the original URL
            cache.Set(
                url,
                url,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                }
            );

            return url;
        }
        catch (Exception ex) when (ex is not LinkPreviewException)
        {
            throw new LinkPreviewException(
                HttpStatusCode.InternalServerError,
                $"Failed to verify URL: {ex.Message}"
            );
        }
    }

    private static bool IsRedirect(HttpStatusCode statusCode) =>
        statusCode
            is HttpStatusCode.Moved
                or HttpStatusCode.Redirect
                or HttpStatusCode.RedirectMethod
                or HttpStatusCode.PermanentRedirect;
}
