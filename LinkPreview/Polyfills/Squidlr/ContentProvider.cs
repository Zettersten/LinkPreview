using LinkPreview.Polyfills.Squidlr.Abstractions;
using LinkPreview.Polyfills.Squidlr.Shared;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LinkPreview.Polyfills.Squidlr;

public sealed class ContentProvider
{
    private readonly IReadOnlyList<IContentProvider> contentProviders;

    private readonly IMemoryCache memoryCache;
    private readonly ILogger<ContentProvider> logger;

    private static readonly Result<Content, RequestContentResult> platformNotSupportedResult =
        new(RequestContentResult.PlatformNotSupported);

    public ContentProvider(
        IReadOnlyList<IContentProvider> contentProviders,
        IMemoryCache memoryCache,
        ILogger<ContentProvider> logger
    )
    {
        ArgumentNullException.ThrowIfNull(contentProviders);
        ArgumentNullException.ThrowIfNull(memoryCache);
        ArgumentNullException.ThrowIfNull(logger);
        if (contentProviders.Count == 0)
        {
            throw new ArgumentException(
                $"No {nameof(IContentProvider)} instances have been provided.",
                nameof(contentProviders)
            );
        }

        this.contentProviders = contentProviders;
        this.memoryCache = memoryCache;
        this.logger = logger;
    }

    public async ValueTask<Result<Content, RequestContentResult>> GetContentAsync(
        ContentIdentifier contentIdentifier,
        CancellationToken cancellationToken
    )
    {
        var cacheKey = $"{contentIdentifier.Platform}-{contentIdentifier.Id}";

        if (
            this.memoryCache.TryGetValue<Result<Content, RequestContentResult>>(
                cacheKey,
                out var result
            )
        )
        {
            return result;
        }

        for (var i = 0; i < this.contentProviders.Count; i++)
        {
            var provider = this.contentProviders[i];
            if (provider.Platform == contentIdentifier.Platform)
            {
                try
                {
                    this.logger.LogInformation(
                        "Loading content from {SocialMediaPlatform}: {ContentId} at {ContentUrl}",
                        contentIdentifier.Platform,
                        contentIdentifier.Id,
                        contentIdentifier.Url
                    );

                    var content = await provider.GetContentAsync(
                        contentIdentifier.Url,
                        cancellationToken
                    );
                    if (content.Error == RequestContentResult.Success)
                    {
                        this.memoryCache.Set(
                            cacheKey,
                            content,
                            absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(60)
                        );
                    }
                    else
                    {
                        if (ShouldBeCached(content.Error))
                        {
                            this.memoryCache.Set(
                                cacheKey,
                                content,
                                absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(60)
                            );
                        }
                    }

                    return content;
                }
                catch (OperationCanceledException)
                {
                    return new(RequestContentResult.Canceled);
                }
                catch (Exception e)
                {
                    this.logger.LogError(
                        e,
                        "An unexpected error occurred while using the {SocialMediaPlatform} content provider.",
                        provider.Platform
                    );

                    return new(RequestContentResult.Error);
                }
            }
        }

        return platformNotSupportedResult;
    }

    private static bool ShouldBeCached(RequestContentResult error)
    {
        return error
            is RequestContentResult.NotFound
                or RequestContentResult.PlatformNotSupported
                or RequestContentResult.NoVideo
                or RequestContentResult.UnsupportedVideo
                or RequestContentResult.AccountSuspended
                or RequestContentResult.Protected
                or RequestContentResult.AdultContent;
    }
}
