using LinkPreview.Polyfills.Squidlr.Abstractions;
using LinkPreview.Polyfills.Squidlr.LinkedIn.Utilities;

namespace LinkPreview.Polyfills.Squidlr.LinkedIn;

public sealed class LinkedInUrlResolver : IUrlResolver
{
    public ContentIdentifier ResolveUrl(string url)
    {
        if (UrlUtilities.TryGetLinkedInIdentifier(url, out var linkedInIdentifier))
            return new ContentIdentifier(
                SocialMediaPlatform.LinkedIn,
                linkedInIdentifier.Value.Id,
                linkedInIdentifier.Value.Url
            );

        return ContentIdentifier.Unknown;
    }
}
