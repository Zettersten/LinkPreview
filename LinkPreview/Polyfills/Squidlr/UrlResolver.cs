using LinkPreview.Polyfills.Squidlr.Abstractions;

namespace LinkPreview.Polyfills.Squidlr;

public sealed class UrlResolver
{
    private readonly IReadOnlyList<IUrlResolver> _urlResolvers;

    public UrlResolver(IReadOnlyList<IUrlResolver> urlResolvers)
    {
        ArgumentNullException.ThrowIfNull(urlResolvers);
        if (urlResolvers.Count == 0)
        {
            throw new ArgumentException(
                $"No {nameof(IUrlResolver)} instances have been provided.",
                nameof(urlResolvers)
            );
        }

        this._urlResolvers = urlResolvers;
    }

    public ContentIdentifier ResolveUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return ContentIdentifier.Unknown;

        for (var i = 0; i < this._urlResolvers.Count; i++)
        {
            var contentIdentifier = this._urlResolvers[i].ResolveUrl(url);
            if (contentIdentifier != ContentIdentifier.Unknown)
                return contentIdentifier;
        }

        return ContentIdentifier.Unknown;
    }

    public bool IsValidUrl(string? url)
    {
        return this.ResolveUrl(url) != ContentIdentifier.Unknown;
    }
}
