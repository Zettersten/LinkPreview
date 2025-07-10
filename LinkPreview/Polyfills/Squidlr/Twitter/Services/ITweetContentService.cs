using LinkPreview.Polyfills.Squidlr.Shared;

namespace LinkPreview.Polyfills.Squidlr.Twitter.Services
{
    public interface ITweetContentService
    {
        ValueTask<Result<TwitterContent, RequestContentResult>> GetTweetContentAsync(
            TweetIdentifier identifier,
            CancellationToken cancellationToken
        );
    }
}
