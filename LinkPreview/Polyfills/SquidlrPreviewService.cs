using LinkPreview.Polyfills.Squidlr;
using LinkPreview.Polyfills.Squidlr.Instagram;
using LinkPreview.Polyfills.Squidlr.Twitter;

namespace LinkPreview.Polyfills;

public sealed class SquidlrPreviewService : PolyfillBase, ILinkPreviewPolyfill
{
    private readonly UrlResolver urlResolver;
    private readonly Squidlr.ContentProvider contentProvider;

    public SquidlrPreviewService(UrlResolver urlResolver, Squidlr.ContentProvider contentProvider)
    {
        this.urlResolver = urlResolver;
        this.contentProvider = contentProvider;
    }

    public bool IsEager => true;

    public int Order => 0;

    public bool CanHandle(string url)
    {
        var resolver = this.urlResolver.ResolveUrl(url);

        return resolver.Platform switch
        {
            SocialMediaPlatform.Twitter => true,
            SocialMediaPlatform.Instagram => true,
            SocialMediaPlatform.Tiktok => true,
            SocialMediaPlatform.LinkedIn => true,
            SocialMediaPlatform.Facebook => true,
            _ => false
        };
    }

    public async Task<LinkPreviewResponse?> TryGetLinkPreviewAsync(
        string url,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var identifier = this.urlResolver.ResolveUrl(url);

            if (identifier.Platform == SocialMediaPlatform.Unknown)
            {
                return null;
            }

            var result = await this.contentProvider.GetContentAsync(identifier, cancellationToken);

            if (!result.IsSuccessful)
            {
                return null;
            }

            if (result.Value is TwitterContent twitterContent)
            {
                return ConvertToLinkPreview(twitterContent);
            }

            if (result.Value is InstagramContent igContent)
            {
                return ConvertToLinkPreview(igContent);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static LinkPreviewResponse? ConvertToLinkPreview(TwitterContent twitterContent)
    {
        if (
            !Uri.TryCreate(
                twitterContent.Videos?.FirstOrDefault()?.DisplayUrl?.ToString() ?? string.Empty,
                UriKind.Absolute,
                out var imageUrl
            )
        )
        {
            return null;
        }

        var imageHeight = 0;
        var imageWidth = 0;

        if (
            twitterContent.Videos?.FirstOrDefault()?.VideoSources?.FirstOrDefault()?.Size
            is not null
        )
        {
            var size = twitterContent.Videos.First().VideoSources.First();

            imageHeight = size.Size.Height;
            imageWidth = size.Size.Width;
        }

        var iconHref = "https://abs.twimg.com/responsive-web/client-web/icon-ios.77d25eba.png";
        var iconWidth = 1024;
        var iconHeight = 1024;

        return new LinkPreviewResponse
        {
            Title = twitterContent.FullText ?? string.Empty,
            Description = twitterContent.FullText ?? string.Empty,
            Url = twitterContent.SourceUrl,
            Image = imageUrl.ToString(),
            ImageHeight = imageHeight,
            ImageWidth = imageWidth,
            Icon = iconHref,
            IconHeight = iconHeight,
            IconWidth = iconWidth,
        };
    }

    private static LinkPreviewResponse? ConvertToLinkPreview(InstagramContent igContent)
    {
        if (
            !Uri.TryCreate(
                igContent.Videos?.FirstOrDefault()?.DisplayUrl?.ToString() ?? string.Empty,
                UriKind.Absolute,
                out var imageUrl
            )
        )
        {
            return null;
        }

        var imageHeight = 0;
        var imageWidth = 0;

        if (igContent.Videos?.FirstOrDefault()?.VideoSources?.FirstOrDefault()?.Size is not null)
        {
            var size = igContent.Videos.First().VideoSources.First();

            imageHeight = size.Size.Height;
            imageWidth = size.Size.Width;
        }

        return new LinkPreviewResponse
        {
            Title = $"A post shared by {igContent.FullName} (@{igContent.Username})",
            Description = igContent.FullText ?? string.Empty,
            Url = igContent.SourceUrl,
            Image = imageUrl.ToString(),
            ImageHeight = imageHeight,
            ImageWidth = imageWidth,
            Icon =
                "https://www.instagram.com/static/images/ico/apple-touch-icon-180x180-precomposed.png/c06fdb2357bd.png",
            IconHeight = 180,
            IconWidth = 180,
        };
    }
}
