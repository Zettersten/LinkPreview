using System.Text.RegularExpressions;
using System.Web;
using ImageSizeReader;

namespace LinkPreview.Polyfills;

/// <summary>
/// Base class for all link preview polyfills, providing user agent rotation and metadata extraction.
/// </summary>
public abstract partial class PolyfillBase
{
    private string? lastUsedUserAgent;
    private int currentUsageCount;
    private readonly Queue<string> userAgentQueue = CreateUserAgentQueue();

    /// <summary>
    /// Rotates through a set of user agents, using each for up to 5 requests.
    /// </summary>
    protected string GetNextUserAgent()
    {
        // Not thread-safe, but fine for most use-cases. Use lock if needed for concurrency.
        if (string.IsNullOrEmpty(this.lastUsedUserAgent))
        {
            this.lastUsedUserAgent = this.userAgentQueue.Dequeue();
            this.userAgentQueue.Enqueue(this.lastUsedUserAgent);
            this.currentUsageCount = 1;
            return this.lastUsedUserAgent;
        }

        if (this.currentUsageCount++ < 5)
        {
            return this.lastUsedUserAgent;
        }

        this.currentUsageCount = 1;
        this.lastUsedUserAgent = this.userAgentQueue.Dequeue();
        this.userAgentQueue.Enqueue(this.lastUsedUserAgent);
        return this.lastUsedUserAgent;
    }

    /// <summary>
    /// Extracts metadata from HTML using a set of regexes.
    /// </summary>
    protected static Metadata GetMetadata(string html)
    {
        static string? GetGroupValue(Match match) =>
            match.Success && match.Groups.Count > 1
                ? CleanAndNormalizeText(match.Groups[1].Value)
                : null;

        var siteTitle = GetGroupValue(SiteTitleRegex().Match(html));
        var siteDescription = GetGroupValue(SiteDescriptionRegex().Match(html));
        var ogTitle = GetGroupValue(OgTitleRegex().Match(html));
        var ogDescription = GetGroupValue(OgDescriptionRegex().Match(html));
        var ogImage = GetGroupValue(OgImageRegex().Match(html))?.Replace("&amp;", "&");
        var twitterTitle = GetGroupValue(TwitterTitleRegex().Match(html));
        var twitterDescription = GetGroupValue(TwitterDescriptionRegex().Match(html));
        var twitterImage = GetGroupValue(TwitterImageRegex().Match(html))?.Replace("&amp;", "&");

        string? username = null,
            displayName = null;
        var match = UsernameMetaRegex().Match(twitterTitle ?? ogTitle ?? siteTitle ?? string.Empty);
        if (match.Success)
        {
            displayName = match.Groups[1].Value.Trim();
            username = match.Groups[2].Value.Trim();
        }

        return new Metadata(
            siteTitle,
            siteDescription,
            ogTitle,
            ogDescription,
            ogImage,
            twitterTitle,
            twitterDescription,
            twitterImage,
            username,
            displayName
        );
    }

    /// <summary>
    /// Cleans and normalizes text by removing newlines, trimming, normalizing whitespace, and decoding HTML entities.
    /// </summary>
    private static string CleanAndNormalizeText(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remove newlines and normalize whitespace
        var cleaned = input.Replace("\r", "").Replace("\n", "");
        cleaned = string.Join(" ", cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return HttpUtility.HtmlDecode(cleaned);
    }

    private static Queue<string> CreateUserAgentQueue()
    {
        return new Queue<string>(
            [
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36 Edg/137.0.0.0",
                "Mozilla/5.0 (Macintosh; U; Intel Mac OS X 8_8_0) Gecko/20100101 Firefox/73.1",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 8_1_5) Gecko/20130401 Firefox/48.7",
                "Mozilla/5.0 (Windows NT 6.1; Win64; x64; en-US) AppleWebKit/536.24 (KHTML, like Gecko) Chrome/51.0.2813.324 Safari/535",
                "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.0;; en-US Trident/5.0)",
                "Mozilla/5.0 (Linux; U; Android 5.1; Nexus 8 Build/LMY48B) AppleWebKit/534.16 (KHTML, like Gecko)  Chrome/53.0.1006.222 Mobile Safari/534.0",
                "Mozilla/5.0 (Android; Android 7.0; GT-I9800 Build/KTU84P) AppleWebKit/537.1 (KHTML, like Gecko)  Chrome/52.0.3068.285 Mobile Safari/535.8",
                "Mozilla/5.0 (Macintosh; U; Intel Mac OS X 9_3_7; en-US) Gecko/20100101 Firefox/65.7",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_8_6; en-US) AppleWebKit/537.24 (KHTML, like Gecko) Chrome/51.0.1015.215 Safari/601",
                "Mozilla/5.0 (Windows; Windows NT 10.4; WOW64; en-US) AppleWebKit/602.50 (KHTML, like Gecko) Chrome/53.0.3549.265 Safari/602.9 Edge/11.98021",
                "Mozilla/5.0 (U; Linux x86_64; en-US) Gecko/20100101 Firefox/62.6"
            ]
        );
    }

    protected async Task<HttpResponseMessage> SendWithRedirectAsync(
        HttpClient client,
        Func<string, string, HttpRequestMessage> createRequest,
        string url,
        CancellationToken cancellationToken
    )
    {
        var ua = this.GetNextUserAgent();
        var request = createRequest(url, ua);
        var response = await client.SendAsync(request, cancellationToken);

        if ((int)response.StatusCode is 301 or 302 or 308)
        {
            var newUrl = response.Headers.Location?.ToString()?.Trim().TrimEnd('#');
            if (!string.IsNullOrEmpty(newUrl))
            {
                request = createRequest(newUrl, ua);
                response = await client.SendAsync(request, cancellationToken);
            }
        }

        return response;
    }

    protected async Task<(int Height, int Width, byte[] Bytes)> DownloadAndExtractImageAsync(
        string imageUrl,
        ImageSizeReaderUtil imageUtils,
        Func<string, string, HttpRequestMessage> createRequest,
        CancellationToken cancellationToken
    )
    {
        var userAgent = this.GetNextUserAgent();
        var imageRequest = createRequest(imageUrl, userAgent);

        using var response = await new HttpClient().SendAsync(imageRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new LinkPreviewException(
                System.Net.HttpStatusCode.InternalServerError,
                $"Image fetch failed: {response.StatusCode}"
            );
        }

        using var imageContent = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var ms = new MemoryStream();

        await imageContent.CopyToAsync(ms, cancellationToken);

        imageContent.Seek(0, SeekOrigin.Begin);

        if (imageContent.Length == 0)
        {
            throw new LinkPreviewException(
                System.Net.HttpStatusCode.InternalServerError,
                "Image response was empty."
            );
        }

        var dimensions = imageUtils.GetDimensions(imageContent);

        ms.Seek(0, SeekOrigin.Begin);

        if (dimensions != null)
        {
            return (dimensions.Height, dimensions.Width, ms.ToArray());
        }

        throw new LinkPreviewException(
            System.Net.HttpStatusCode.InternalServerError,
            "Unsupported image format."
        );
    }

    // Regex generators
    [GeneratedRegex(@"<meta property=""description"" content=""(.*?)""", RegexOptions.Singleline)]
    private static partial Regex SiteDescriptionRegex();

    [GeneratedRegex(
        @"<title>(.*?)<\/title>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline,
        "en-US"
    )]
    private static partial Regex SiteTitleRegex();

    [GeneratedRegex(
        @"<meta property=""og:description"" content=""(.*?)""",
        RegexOptions.IgnoreCase | RegexOptions.Singleline,
        "en-US"
    )]
    private static partial Regex OgDescriptionRegex();

    [GeneratedRegex(
        @"<meta property=""og:title"" content=""(.*?)""",
        RegexOptions.IgnoreCase | RegexOptions.Singleline,
        "en-US"
    )]
    private static partial Regex OgTitleRegex();

    [GeneratedRegex(
        @"<meta property=""og:image"" content=""(.*?)""",
        RegexOptions.IgnoreCase | RegexOptions.Singleline,
        "en-US"
    )]
    private static partial Regex OgImageRegex();

    [GeneratedRegex(
        @"<meta\s+name=""twitter:title""\s+content=""(.*?)""\s*\/?>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline,
        "en-US"
    )]
    private static partial Regex TwitterTitleRegex();

    [GeneratedRegex(
        @"<meta\s+name=""twitter:image""\s+content=""(.*?)""\s*\/?>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline,
        "en-US"
    )]
    private static partial Regex TwitterImageRegex();

    [GeneratedRegex(
        @"<meta\s+name=""twitter:description""\s+content=""(.*?)""\s*\/?>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline,
        "en-US"
    )]
    private static partial Regex TwitterDescriptionRegex();

    // Matches: Display Name (@username) or Display Name (&#064;username)
    [GeneratedRegex(@"^(.*?)\s+\((?:&#064;|@)([a-zA-Z0-9_]+)\)", RegexOptions.Singleline)]
    private static partial Regex UsernameMetaRegex();

    /// <summary>
    /// Metadata record for extracted HTML meta tags.
    /// </summary>
    protected record Metadata(
        string? SiteTitle,
        string? SiteDescription,
        string? OgTitle,
        string? OgDescription,
        string? OgImage,
        string? TwitterTitle,
        string? TwitterDescription,
        string? TwitterImage,
        string? Username,
        string? DisplayName
    );
}
