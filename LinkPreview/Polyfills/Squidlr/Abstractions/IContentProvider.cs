using LinkPreview.Polyfills.Squidlr.Shared;

namespace LinkPreview.Polyfills.Squidlr.Abstractions;

public interface IContentProvider
{
    SocialMediaPlatform Platform { get; }

    ValueTask<Result<Content, RequestContentResult>> GetContentAsync(
        string url,
        CancellationToken cancellationToken
    );
}
