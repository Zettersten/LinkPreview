using LinkPreview.Polyfills.Squidlr.Abstractions;
using LinkPreview.Polyfills.Squidlr.Facebook.Utilities;

namespace LinkPreview.Polyfills.Squidlr.Facebook;

public sealed class FacebookUrlResolver : IUrlResolver
{
    public ContentIdentifier ResolveUrl(string url)
    {
        if (UrlUtilities.TryGetFacebookIdentifier(url, out var facebookIdentifier))
            return new ContentIdentifier(
                SocialMediaPlatform.Facebook,
                facebookIdentifier.Value.Id,
                facebookIdentifier.Value.Url
            );

        return ContentIdentifier.Unknown;
    }
}
