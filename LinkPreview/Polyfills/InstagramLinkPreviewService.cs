using System.Text.RegularExpressions;
using System.Web;
using ImageSizeReader;

namespace LinkPreview.Polyfills;

public partial class InstagramLinkPreviewService : ILinkPreviewService
{
    private readonly Queue<string> userAgentQueue;
    private readonly ImageSizeReaderUtil imageUtils;
    private readonly HttpClient httpClient;

    public InstagramLinkPreviewService()
    {
        this.httpClient = new HttpClient(
            new HttpClientHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression =
                    System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
                PreAuthenticate = false,
                UseCookies = false,
                UseProxy = false,
                Proxy = null,
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            }
        )
        {
            BaseAddress = new Uri("https://www.instagram.com/"),
        };

        this.userAgentQueue = CreateUserAgentQueue();
        this.imageUtils = new ImageSizeReaderUtil();
    }

    public async Task<LinkPreviewResponse> GetLinkPreviewAsync(
        string url,
        LinkPreviewOptionalField? optionalFields = null,
        CancellationToken cancellationToken = default
    )
    {
        if (!IsValidInstagramUrl(url))
        {
            throw new LinkPreviewException(
                System.Net.HttpStatusCode.InternalServerError,
                "URL provided was not instagram."
            );
        }

        var userAgent = this.GetNextUserAgentIfRequired();
        var requestMessage = CreateInstagramHtmlRequestMessage(url, userAgent);
        var response = await this.httpClient.SendAsync(requestMessage, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new LinkPreviewException(
                System.Net.HttpStatusCode.InternalServerError,
                $"Instagram failed to respond. Their status code: {response.StatusCode}"
            );
        }

        var currentCount = this.CurrentUsageCount;
        var newCount = Interlocked.Increment(ref currentCount);

        this.CurrentUsageCount = newCount;

        var htmlContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrEmpty(htmlContent))
        {
            throw new LinkPreviewException(
                System.Net.HttpStatusCode.InternalServerError,
                "Instagram response was empty."
            );
        }

        var linkPreviewResponse = ParseInstagramResponse(htmlContent);

        var dimensions = await this.GetImageDimensions(linkPreviewResponse, cancellationToken);

        linkPreviewResponse.ImageHeight = dimensions.ImageHeight;
        linkPreviewResponse.ImageWidth = dimensions.ImageWidth;
        linkPreviewResponse.ImageType = Convert.ToBase64String(dimensions.ImageBytes);

        return linkPreviewResponse;
    }

    private string GetNextUserAgentIfRequired()
    {
        if (this.CurrentUsageCount % 5 == 0)
        {
            var userAgent = this.userAgentQueue.Dequeue();
            this.userAgentQueue.Enqueue(userAgent);

            return userAgent;
        }

        var defaultUserAgent = this.userAgentQueue.Dequeue();
        this.userAgentQueue.Enqueue(defaultUserAgent);

        return defaultUserAgent;
    }

    private int CurrentUsageCount { get; set; } = 0;

    private static bool IsValidInstagramUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            return false;
        }

        if (
            !url.StartsWith("https://www.instagram.com/")
            && !url.StartsWith("https://instagram.com/")
        )
        {
            return false;
        }

        return true;
    }

    private static HttpRequestMessage CreateInstagramHtmlRequestMessage(
        string url,
        string userAgent
    )
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Version = new Version(2, 0)
        };

        requestMessage.Headers.Add(
            "sec-ch-ua",
            "\"Microsoft Edge\";v=\"135\", \"Not-A.Brand\";v=\"8\", \"Chromium\";v=\"135\""
        );
        requestMessage.Headers.Add("sec-ch-ua-mobile", "?0");
        requestMessage.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
        requestMessage.Headers.Add("dnt", "1");
        requestMessage.Headers.Add("upgrade-insecure-requests", "1");

        requestMessage.Headers.Add("user-agent", userAgent);

        requestMessage.Headers.Add(
            "accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7"
        );

        requestMessage.Headers.Add("sec-fetch-site", "none");
        requestMessage.Headers.Add("sec-fetch-mode", "navigate");
        requestMessage.Headers.Add("sec-fetch-user", "?1");
        requestMessage.Headers.Add("sec-fetch-dest", "document");
        requestMessage.Headers.Add("accept-language", "en-US,en;q=0.9");
        requestMessage.Headers.Add("priority", "u=0, i");

        return requestMessage;
    }

    private static HttpRequestMessage CreateInstagramImageRequestMessage(
        string url,
        string userAgent
    )
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Version = new Version(2, 0)
        };
        requestMessage.Headers.Add(
            "sec-ch-ua",
            "\"Microsoft Edge\";v=\"135\", \"Not-A.Brand\";v=\"8\", \"Chromium\";v=\"135\""
        );
        requestMessage.Headers.Add("sec-ch-ua-mobile", "?0");
        requestMessage.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
        requestMessage.Headers.Add("dnt", "1");
        requestMessage.Headers.Add("upgrade-insecure-requests", "1");
        requestMessage.Headers.Add("user-agent", userAgent);
        requestMessage.Headers.Add(
            "accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7"
        );
        requestMessage.Headers.Add("accept-language", "en-US,en;q=0.9");
        requestMessage.Headers.Add("cache-control", "no-cache");
        requestMessage.Headers.Add("pragma", "no-cache");
        requestMessage.Headers.Add("sec-fetch-site", "none");
        requestMessage.Headers.Add("sec-fetch-mode", "navigate");
        requestMessage.Headers.Add("sec-fetch-user", "?1");
        requestMessage.Headers.Add("sec-fetch-dest", "document");
        requestMessage.Headers.Add("priority", "u=0, i");

        return requestMessage;
    }

    public static LinkPreviewResponse ParseInstagramResponse(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            throw new ArgumentException(
                "HTML content cannot be null or empty.",
                nameof(htmlContent)
            );
        }

        var descriptionRegex = SocialDescriptionMetaRegex();
        var urlRegex = UrlMetaRegex();
        var imageRegex = ImageMetaRegex();
        var twitterRegex = TwitterMetaRegex();

        var descriptionMatch = descriptionRegex.Match(htmlContent);
        var urlMatch = urlRegex.Match(htmlContent);
        var imageMatch = imageRegex.Match(htmlContent);
        var twitterMatch = twitterRegex.Match(htmlContent);

        var description = CleanAndNormalizeText(
            descriptionMatch.Success ? descriptionMatch.Groups[1].Value : string.Empty
        );

        var url = urlMatch.Success ? urlMatch.Groups[1].Value : string.Empty;
        var image = (imageMatch.Success ? imageMatch.Groups[1].Value : string.Empty).Replace(
            "&amp;",
            "&"
        );

        if (
            !TryParseUsernameAndDisplayName(
                twitterMatch.Success ? twitterMatch.Groups[1].Value : string.Empty,
                out var username,
                out var displayName
            )
        )
        {
            throw new LinkPreviewException(
                System.Net.HttpStatusCode.InternalServerError,
                "Could not parse username or display name."
            );
        }

        if (!TryParseDescription(description, out var metaDescription))
        {
            throw new LinkPreviewException(
                System.Net.HttpStatusCode.InternalServerError,
                "Could not parse description."
            );
        }

        var linkPreviewTitle = $"A post shared by {displayName} (@{username})";

        return new LinkPreviewResponse
        {
            Title = linkPreviewTitle,
            Description = metaDescription,
            Url = url,
            Image = image,
            ImageSize = null,
            ImageType = null,
            ImageWidth = null,
            ImageHeight = null
        };
    }

    private static bool TryParseUsernameAndDisplayName(
        string input,
        out string username,
        out string displayName
    )
    {
        username = string.Empty;
        displayName = string.Empty;

        var regex = UsernameMetaRegex();
        var match = regex.Match(input);

        if (match.Success)
        {
            displayName = match.Groups[1].Value.Trim();
            username = match.Groups[2].Value.Trim();
            return true;
        }

        return false;
    }

    public static bool TryParseDescription(string input, out string description)
    {
        description = string.Empty;
        var regex = DescriptionMetaRegex();
        var matches = regex.Matches(input);

        if (matches.Count > 0)
        {
            description = matches[0].Groups[1].Value;
            return true;
        }

        return false;
    }

    private static string CleanAndNormalizeText(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        // Step 1: Remove new line characters
        input = input.Replace("\r", "").Replace("\n", "");

        // Step 2: Trim and normalize white spaces
        input = string.Join(" ", input.Split([' '], StringSplitOptions.RemoveEmptyEntries));

        // Step 3: Encode HTML special characters
        input = HttpUtility.HtmlDecode(input);

        return input;
    }

    private static Queue<string> CreateUserAgentQueue()
    {
        var userAgentQueue = new Queue<string>();

        userAgentQueue.Enqueue(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36 Edg/135.0.0.0"
        );
        userAgentQueue.Enqueue(
            "Mozilla/5.0 (Macintosh; U; Intel Mac OS X 8_8_0) Gecko/20100101 Firefox/73.1"
        );
        userAgentQueue.Enqueue(
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 8_1_5) Gecko/20130401 Firefox/48.7"
        );
        userAgentQueue.Enqueue(
            "Mozilla/5.0 (Windows NT 6.1; Win64; x64; en-US) AppleWebKit/536.24 (KHTML, like Gecko) Chrome/51.0.2813.324 Safari/535"
        );
        userAgentQueue.Enqueue(
            "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.0;; en-US Trident/5.0)"
        );
        userAgentQueue.Enqueue(
            "Mozilla/5.0 (Linux; U; Android 5.1; Nexus 8 Build/LMY48B) AppleWebKit/534.16 (KHTML, like Gecko)  Chrome/53.0.1006.222 Mobile Safari/534.0"
        );
        userAgentQueue.Enqueue(
            "Mozilla/5.0 (Android; Android 7.0; GT-I9800 Build/KTU84P) AppleWebKit/537.1 (KHTML, like Gecko)  Chrome/52.0.3068.285 Mobile Safari/535.8"
        );
        userAgentQueue.Enqueue(
            "Mozilla/5.0 (Macintosh; U; Intel Mac OS X 9_3_7; en-US) Gecko/20100101 Firefox/65.7"
        );
        userAgentQueue.Enqueue(
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_8_6; en-US) AppleWebKit/537.24 (KHTML, like Gecko) Chrome/51.0.1015.215 Safari/601"
        );
        userAgentQueue.Enqueue(
            "Mozilla/5.0 (Windows; Windows NT 10.4; WOW64; en-US) AppleWebKit/602.50 (KHTML, like Gecko) Chrome/53.0.3549.265 Safari/602.9 Edge/11.98021"
        );
        userAgentQueue.Enqueue("Mozilla/5.0 (U; Linux x86_64; en-US) Gecko/20100101 Firefox/62.6");

        return userAgentQueue;
    }

    [GeneratedRegex("\"(.*?)\"", RegexOptions.Singleline)]
    private static partial Regex DescriptionMetaRegex();

    [GeneratedRegex(@"^(.*?)\s+\(&#064;([a-zA-Z0-9_]+)\)", RegexOptions.Singleline)]
    private static partial Regex UsernameMetaRegex();

    [GeneratedRegex(
        @"<title>(.*?)<\/title>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline,
        "en-US"
    )]
    private static partial Regex TitleMetaRegex();

    [GeneratedRegex(
        @"<meta property=""og:description"" content=""(.*?)""",
        RegexOptions.IgnoreCase | RegexOptions.Singleline,
        "en-US"
    )]
    private static partial Regex SocialDescriptionMetaRegex();

    [GeneratedRegex(
        @"<meta property=""og:url"" content=""(.*?)""",
        RegexOptions.IgnoreCase | RegexOptions.Singleline,
        "en-US"
    )]
    private static partial Regex UrlMetaRegex();

    [GeneratedRegex(
        @"<meta property=""og:image"" content=""(.*?)""",
        RegexOptions.IgnoreCase | RegexOptions.Singleline,
        "en-US"
    )]
    private static partial Regex ImageMetaRegex();

    [GeneratedRegex(
        @"<meta\s+name=""twitter:title""\s+content=""(.*?)""\s*\/?>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline,
        "en-US"
    )]
    private static partial Regex TwitterMetaRegex();

    private async Task<(int ImageHeight, int ImageWidth, byte[] ImageBytes)> GetImageDimensions(
        LinkPreviewResponse linkPreviewResponse,
        CancellationToken cancellationToken
    )
    {
        // Your existing code to get the image bytes
        var userAgent = this.GetNextUserAgentIfRequired();
        var imageRequest = CreateInstagramImageRequestMessage(linkPreviewResponse.Image, userAgent);
        var response = await this.httpClient.SendAsync(imageRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new LinkPreviewException(
                System.Net.HttpStatusCode.InternalServerError,
                $"Instagram failed to respond. Their status code: {response.StatusCode}"
            );
        }

        using var imageContent = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var ms = new MemoryStream();

        await imageContent.CopyToAsync(ms, cancellationToken);

        imageContent.Seek(0, SeekOrigin.Begin);

        if (imageContent == null || imageContent.Length == 0)
        {
            throw new LinkPreviewException(
                System.Net.HttpStatusCode.InternalServerError,
                "Instagram response was empty."
            );
        }

        // Determine the image format
        var dimensions = this.imageUtils.GetDimensions(imageContent);

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
}
