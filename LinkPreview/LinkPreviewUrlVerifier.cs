using System.Net;
using LinkPreview.Polyfills;
using Microsoft.Extensions.Caching.Memory;

namespace LinkPreview;

public sealed class LinkPreviewUrlVerifier(HttpClient httpClient, IMemoryCache cache)
{
    private static readonly Queue<string> userAgentQueue = RegexUtilities.CreateUserAgentQueue(
        botAgentsOnly: true
    );

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

        var currentRetryCount = 0;
        var maxRetries = userAgentQueue.Count;

        while (currentRetryCount < maxRetries)
        {
            var currentUserAgent = userAgentQueue.Dequeue();
            userAgentQueue.Enqueue(currentUserAgent); // Recycle the user agent

            try
            {
                var urlPlusTs =
                    $"{url}{(url.Contains('?') ? '&' : '?')}ts={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

                using var request = new HttpRequestMessage(HttpMethod.Get, urlPlusTs);
                request.Headers.UserAgent.ParseAdd(currentUserAgent);

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
                    var responseContent = await response.Content.ReadAsStringAsync(
                        cancellationToken
                    );

                    if (
                        responseContent.Contains(
                            "This browser is no longer supported",
                            StringComparison.OrdinalIgnoreCase
                        )
                        || responseContent.Contains(
                            "Looks like this page doesn’t exist",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        currentRetryCount++;
                        continue;
                    }

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
            catch (LinkPreviewException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                currentRetryCount++;
            }
            catch (Exception ex) when (ex is not LinkPreviewException)
            {
                throw new LinkPreviewException(
                    HttpStatusCode.InternalServerError,
                    $"Failed to verify URL: {ex.Message}"
                );
            }
        }

        throw new LinkPreviewException(
            HttpStatusCode.TooManyRequests,
            "Maximum retry attempts reached while verifying URL."
        );
    }

    private static bool IsRedirect(HttpStatusCode statusCode) =>
        statusCode
            is HttpStatusCode.Moved
                or HttpStatusCode.Redirect
                or HttpStatusCode.RedirectMethod
                or HttpStatusCode.PermanentRedirect;
}
