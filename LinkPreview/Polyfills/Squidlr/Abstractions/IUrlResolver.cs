namespace LinkPreview.Polyfills.Squidlr.Abstractions;

public interface IUrlResolver
{
    ContentIdentifier ResolveUrl(string url);
}
