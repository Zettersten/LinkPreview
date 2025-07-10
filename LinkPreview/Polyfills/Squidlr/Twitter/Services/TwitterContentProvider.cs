using LinkPreview.Polyfills.Squidlr.Abstractions;
using LinkPreview.Polyfills.Squidlr.Shared;
using LinkPreview.Polyfills.Squidlr.Twitter.Utilities;

namespace LinkPreview.Polyfills.Squidlr.Twitter.Services;

public class TwitterContentProvider : IContentProvider
{
    private readonly TweetContentParserFactory _tweetContentParserFactory;

    public SocialMediaPlatform Platform { get; } = SocialMediaPlatform.Twitter;

    public TwitterContentProvider(TweetContentParserFactory tweetContentParserFactory)
    {
        this._tweetContentParserFactory =
            tweetContentParserFactory
            ?? throw new ArgumentNullException(nameof(tweetContentParserFactory));
    }

    public async ValueTask<Result<Content, RequestContentResult>> GetContentAsync(
        string url,
        CancellationToken cancellationToken
    )
    {
        var identifier = UrlUtilities.CreateTweetIdentifierFromUrl(url);
        var parser = this._tweetContentParserFactory.CreateTweetContentParser(identifier);
        var result = await parser.CreateTweetContentAsync(cancellationToken);
        if (result.IsSuccessful)
        {
            return new(result.Value);
        }

        return new(result.Error);
    }
}
