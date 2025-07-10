using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LinkPreview.Polyfills.Squidlr.Twitter;

public sealed class TweetContentParserFactory
{
    private readonly IServiceProvider _serviceProvider;

    public TweetContentParserFactory(IServiceProvider serviceProvider)
    {
        this._serviceProvider =
            serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public TweetContentParser CreateTweetContentParser(TweetIdentifier tweetIdentifier)
    {
        var twitterClient = this._serviceProvider.GetRequiredService<TwitterWebClient>();
        var logger = this._serviceProvider.GetRequiredService<ILogger<TweetContentParser>>();

        return new TweetContentParser(tweetIdentifier, twitterClient, logger);
    }
}
