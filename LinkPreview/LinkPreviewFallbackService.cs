using LinkPreview.Polyfills;
using Microsoft.Extensions.Caching.Memory;

namespace LinkPreview;

public sealed class LinkPreviewFallbackService(HttpClient httpClient, IMemoryCache cache)
{
    private const string cacheKeyPrefix = "LinkPreviewFallback_";

    private readonly Queue<string> userAgentQueue = RegexUtilities.CreateUserAgentQueue(
        botAgentsOnly: true
    );

    public async Task<LinkPreviewResponse?> TryGetLinkPreviewAsFallback(
        string url,
        CancellationToken cancellationToken = default
    )
    {
        if (
            cache.TryGetValue(cacheKeyPrefix + url, out var cachedLinkPreview)
            && cachedLinkPreview is LinkPreviewResponse linkPreview
        )
        {
            return linkPreview;
        }

        var currentRetryCount = 0;
        var maxRetries = this.userAgentQueue.Count;

        while (currentRetryCount < maxRetries)
        {
            var userAgent = this.userAgentQueue.Dequeue();
            this.userAgentQueue.Enqueue(userAgent); // Recycle the user agent

            try
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                requestMessage.Headers.UserAgent.ParseAdd(userAgent);
                requestMessage.Version = new Version(2, 0);
                requestMessage.VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

                var response = await httpClient.SendAsync(requestMessage, cancellationToken);

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                var metadata = RegexUtilities.GetMetadata(content, url);

                var image =
                    metadata.OgImage ?? metadata.TwitterImage ?? metadata.Favicon ?? string.Empty;
                var title =
                    metadata.SiteTitle ?? metadata.TwitterTitle ?? metadata.OgTitle ?? string.Empty;
                var description =
                    metadata.OgDescription
                    ?? metadata.TwitterDescription
                    ?? metadata.SiteDescription
                    ?? string.Empty;

                if (
                    string.IsNullOrEmpty(title)
                    && string.IsNullOrEmpty(description)
                    && string.IsNullOrEmpty(image)
                )
                {
                    // If no metadata is found, return null
                    return null;
                }

                var result = new LinkPreviewResponse
                {
                    Title = title,
                    Description = description,
                    Url = url,
                    Image = image,
                    Icon = metadata.Favicon,
                    ImageSize = null,
                    ImageType = null,
                    ImageWidth = null,
                    ImageHeight = null
                };

                cache.Set(
                    cacheKeyPrefix + url,
                    result,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                    }
                );

                return result;
            }
            catch
            {
                currentRetryCount++;

                if (currentRetryCount >= maxRetries)
                {
                    return null;
                }
            }
        }

        return null;
    }
}
