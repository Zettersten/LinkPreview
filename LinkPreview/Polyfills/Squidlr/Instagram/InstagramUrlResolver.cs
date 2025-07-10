using LinkPreview.Polyfills.Squidlr.Abstractions;
using LinkPreview.Polyfills.Squidlr.Instagram.Utilities;

namespace LinkPreview.Polyfills.Squidlr.Instagram;

public sealed class InstagramUrlResolver : IUrlResolver
{
    public ContentIdentifier ResolveUrl(string url)
    {
        if (UrlUtilities.TryGetInstagramIdentifier(url, out var instagramIdentifier))
            return new ContentIdentifier(
                SocialMediaPlatform.Instagram,
                instagramIdentifier.Value.Id,
                instagramIdentifier.Value.Url
            );

        return ContentIdentifier.Unknown;
    }
}
