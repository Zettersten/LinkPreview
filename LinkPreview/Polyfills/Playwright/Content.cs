using LinkPreview.Polyfills.Squidlr;

namespace LinkPreview.Polyfills.Playwright;

public sealed record class Content(string HtmlContent, ContentIdentifier ContentIdentifier);
