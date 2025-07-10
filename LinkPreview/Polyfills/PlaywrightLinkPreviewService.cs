using HtmlAgilityPack;
using ImageSizeReader;
using LinkPreview.Polyfills.Squidlr;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;

namespace LinkPreview.Polyfills;

public class PlaywrightLinkPreviewService : PolyfillBase, ILinkPreviewPolyfill
{
    private readonly Playwright.ContentProvider contentProvider;
    private readonly UrlResolver urlResolver;
    private readonly IMemoryCache memoryCache;
    private readonly HttpClient httpClient;
    private readonly ImageSizeReaderUtil imageUtils;

    public PlaywrightLinkPreviewService(
        Playwright.ContentProvider contentProvider,
        UrlResolver urlResolver,
        IMemoryCache memoryCache
    )
    {
        this.contentProvider = contentProvider;
        this.urlResolver = urlResolver;
        this.memoryCache = memoryCache;

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

        this.imageUtils = new ImageSizeReaderUtil();
    }

    public bool IsEager => false;

    public int Order => 3;

    public bool CanHandle(string url)
    {
        var provider = this.urlResolver.ResolveUrl(url);

        return provider.Platform switch
        {
            SocialMediaPlatform.Twitter => true,
            SocialMediaPlatform.Instagram => true,
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
            var pageContent = await this.contentProvider.GetContent(url, cancellationToken);

            return pageContent.ContentIdentifier.Platform switch
            {
                SocialMediaPlatform.Twitter => this.ConvertToLinkTwitterPreview(pageContent),
                SocialMediaPlatform.Instagram
                    => await this.ConvertToLinkInstagramPreview(pageContent, cancellationToken),
                _ => null
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    private LinkPreviewResponse? ConvertToLinkTwitterPreview(Playwright.Content pageContent)
    {
        var cacheKey =
            $"{nameof(PlaywrightLinkPreviewService)}-{pageContent.ContentIdentifier.Platform}-{pageContent.ContentIdentifier.Id}";

        if (
            this.memoryCache.TryGetValue<LinkPreviewResponse>(cacheKey, out var result)
            && result is not null
        )
        {
            return result;
        }

        var startIndex = pageContent.HtmlContent.IndexOf("<div id=\"react-root\"");
        var endIndex = pageContent.HtmlContent.IndexOf("<script nonce=\"\">document.cookie=");

        var htmlToParse = (startIndex, endIndex) switch
        {
            (-1, _) => null, // Start marker not found
            (var start, -1) => pageContent.HtmlContent[start..], // End marker not found
            (var start, var end) => pageContent.HtmlContent[start..end] // Both found
        };

        if (htmlToParse is null)
        {
            return null;
        }

        var doc = new HtmlDocument();

        doc.LoadHtml(htmlToParse);

        var testNodes = doc
            .DocumentNode.Descendants("div")
            .Where(x => x.Attributes.Contains("data-testid"));

        var textNode = testNodes.FirstOrDefault(x => x.Attributes.Any(x => x.Value == "tweetText"));

        if (textNode is null)
        {
            return null;
        }

        var imageNode = testNodes.FirstOrDefault(x =>
            x.Attributes.Any(x => x.Value == "tweetPhoto")
        );

        if (imageNode is null)
        {
            return null;
        }

        var imageHref = imageNode
            .Descendants("img")
            .FirstOrDefault()
            ?.Attributes["src"]
            ?.Value?.Replace("&amp;", "&");

        if (!Uri.TryCreate(imageHref, UriKind.Absolute, out var imageUri))
        {
            return null;
        }

        var usernameNode = testNodes.FirstOrDefault(x =>
            x.Attributes.Any(x => x.Value == "User-Name")
        );

        if (usernameNode is null)
        {
            return null;
        }

        var emojisToReplace = textNode
            .Descendants("img")
            .Where(x => x.Attributes.Any(x => x.Value.EndsWith("svg")))
            .Select(x => (OldNode: x, NewNode: HtmlNode.CreateNode(x.Attributes["alt"].Value)))
            .ToList();

        foreach (var (OldNode, NewNode) in emojisToReplace)
        {
            textNode.ReplaceChild(NewNode, OldNode);
        }

        var description = textNode.InnerText;
        var image = imageUri.ToString();
        var imageSize = QueryHelpers.ParseQuery(imageUri.Query)["name"].FirstOrDefault();
        var imageHeight = int.Parse(imageSize?.Split("x")[0] ?? "640");
        var imageWidth = int.Parse(imageSize?.Split("x")[1] ?? "640");
        var usernameText = usernameNode.InnerText.Trim().Split('@');
        var displayName = usernameText[0];
        var username = usernameText[1];
        var iconHref = "https://abs.twimg.com/responsive-web/client-web/icon-ios.77d25eba.png";
        var iconWidth = 1024;
        var iconHeight = 1024;
        var title = $"{displayName} on X";

        var content = new LinkPreviewResponse
        {
            Title = title,
            Icon = iconHref,
            IconHeight = iconHeight,
            IconWidth = iconWidth,
            Image = image,
            ImageHeight = imageHeight,
            ImageWidth = imageWidth,
            IsPolyfill = true,
            Description = description,
            Url = pageContent.ContentIdentifier.Url
        };

        if (content is not null and { Title: not null, Description: not null, Image: not null })
        {
            this.memoryCache.Set(
                cacheKey,
                content,
                absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(60)
            );

            return content;
        }

        return null;
    }

    private async Task<LinkPreviewResponse?> ConvertToLinkInstagramPreview(
        Playwright.Content pageContent,
        CancellationToken cancellationToken = default
    )
    {
        var cacheKey =
            $"{nameof(PlaywrightLinkPreviewService)}-{pageContent.ContentIdentifier.Platform}-{pageContent.ContentIdentifier.Id}";

        if (
            this.memoryCache.TryGetValue<LinkPreviewResponse>(cacheKey, out var result)
            && result is not null
        )
        {
            return result;
        }

        var htmlToParse = pageContent.HtmlContent;

        if (htmlToParse is null)
        {
            return null;
        }

        var doc = new HtmlDocument();

        doc.LoadHtml(htmlToParse);

        var tileNodes = doc
            .DocumentNode.Descendants("div")
            .Where(x => x.HasClass("html-div"))
            .SelectMany(x =>
                x.Descendants("a")
                    .Where(x =>
                        x.Attributes["href"].Value.StartsWith('/')
                        && x.Attributes["href"].Value.EndsWith('/')
                        && x.Attributes["href"].Value.Length > 3
                        && x.Attributes["role"].Value == "link"
                    )
                    .Where(x =>
                        x.Descendants("span").Any(x => x?.Attributes["dir"]?.Value == "auto")
                    )
                    .Select(x => x.ParentNode.ParentNode.ParentNode)
            )
            .Skip(1)
            .FirstOrDefault();

        var username = tileNodes
            ?.Descendants("a")
            .FirstOrDefault()
            ?.Descendants("span")
            ?.FirstOrDefault()
            ?.InnerText;

        var description = tileNodes
            ?.ChildNodes?.FirstOrDefault()
            ?.ChildNodes?.Skip(1)
            ?.FirstOrDefault()
            ?.InnerText?.ReplaceLineEndings()
            ?.Replace("\r\n", " ");

        if (string.IsNullOrEmpty(description))
        {
            return null;
        }

        var image = doc
            .DocumentNode.Descendants("img")
            .Where(x => x.Attributes["alt"].Value.StartsWith("Photo by "))
            .FirstOrDefault()
            ?.Attributes["src"]
            .Value;

        if (image is null || !Uri.TryCreate(image, UriKind.Absolute, out var imageUri))
        {
            return null;
        }

        var content = new LinkPreviewResponse
        {
            Title = $"A post shared by {username} (@{username})",
            Description = description,
            Icon =
                "https://www.instagram.com/static/images/ico/apple-touch-icon-180x180-precomposed.png/c06fdb2357bd.png",
            IconHeight = 180,
            IconWidth = 180,
            Image = imageUri.ToString(),
            ImageHeight = 1350,
            ImageWidth = 1080,
            IsPolyfill = true
        };

        var (ImageHeight, ImageWidth, ImageBytes) = await this.DownloadAndExtractImageAsync(
            imageUri.ToString(),
            this.httpClient,
            this.imageUtils,
            CreateInstagramImageRequestMessage,
            cancellationToken
        );

        content.ImageHeight = ImageHeight;
        content.ImageWidth = ImageWidth;
        content.ImageType = Convert.ToBase64String(ImageBytes);

        if (content is not null and { Title: not null, Description: not null, Image: not null })
        {
            this.memoryCache.Set(
                cacheKey,
                content,
                absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(60)
            );

            return content;
        }

        return null;
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
}
