using ImageSizeReader;

namespace LinkPreview.Polyfills;

public partial class InstagramLinkPreviewService : PolyfillBase, ILinkPreviewPolyfill
{
    private readonly IImageSizeReaderUtil imageUtils;
    private readonly HttpClient httpClient;

    public InstagramLinkPreviewService(IImageSizeReaderUtil imageSizeReaderUtil)
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

        this.imageUtils = imageSizeReaderUtil;
    }

    public int Order => 1;

    public bool IsEager => true;

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

    public static LinkPreviewResponse ParseInstagramResponse(string url, string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            throw new ArgumentException(
                "HTML content cannot be null or empty.",
                nameof(htmlContent)
            );
        }

        var metadata = RegexUtilities.GetMetadata(htmlContent, "https://instagram.com");

        if (string.IsNullOrEmpty(metadata.Username) || string.IsNullOrEmpty(metadata.DisplayName))
        {
            throw new LinkPreviewException(
                System.Net.HttpStatusCode.InternalServerError,
                "Could not parse username or display name."
            );
        }

        if (
            string.IsNullOrEmpty(metadata.OgDescription)
            && string.IsNullOrEmpty(metadata.SiteDescription)
            && string.IsNullOrEmpty(metadata.TwitterDescription)
        )
        {
            throw new LinkPreviewException(
                System.Net.HttpStatusCode.InternalServerError,
                "Could not parse description."
            );
        }

        var linkPreviewTitle = $"A post shared by {metadata.DisplayName} (@{metadata.Username})";

        return new LinkPreviewResponse
        {
            Title = linkPreviewTitle,
            Description =
                metadata.OgDescription
                ?? metadata.TwitterDescription
                ?? metadata.SiteDescription
                ?? string.Empty,
            Url = url,
            Image = metadata.OgImage ?? metadata.TwitterImage ?? metadata.Favicon ?? string.Empty,
            Icon = metadata.Favicon,
            ImageSize = null,
            ImageType = null,
            ImageWidth = null,
            ImageHeight = null
        };
    }

    public bool CanHandle(string url) => IsValidInstagramUrl(url);

    public async Task<LinkPreviewResponse?> TryGetLinkPreviewAsync(
        string url,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (!IsValidInstagramUrl(url))
            {
                throw new LinkPreviewException(
                    System.Net.HttpStatusCode.InternalServerError,
                    "URL provided was not instagram."
                );
            }

            var response = await this.SendWithRedirectAsync(
                this.httpClient,
                CreateInstagramHtmlRequestMessage,
                url,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                throw new LinkPreviewException(
                    System.Net.HttpStatusCode.InternalServerError,
                    $"Failed to fetch HTML: {response.StatusCode}"
                );
            }

            var htmlContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrEmpty(htmlContent))
            {
                throw new LinkPreviewException(
                    System.Net.HttpStatusCode.InternalServerError,
                    "Instagram response was empty."
                );
            }

            var linkPreviewResponse = ParseInstagramResponse(url, htmlContent);

            var (ImageHeight, ImageWidth, ImageBytes) = await this.DownloadAndExtractImageAsync(
                linkPreviewResponse.Image,
                this.httpClient,
                this.imageUtils,
                CreateInstagramImageRequestMessage,
                cancellationToken
            );

            linkPreviewResponse.ImageHeight = ImageHeight;
            linkPreviewResponse.ImageWidth = ImageWidth;
            linkPreviewResponse.ImageType = Convert.ToBase64String(ImageBytes);
            linkPreviewResponse.Icon =
                "https://www.instagram.com/static/images/ico/apple-touch-icon-180x180-precomposed.png/c06fdb2357bd.png";
            linkPreviewResponse.IconHeight = 180;
            linkPreviewResponse.IconWidth = 180;

            return linkPreviewResponse;
        }
        catch
        {
            return null;
        }
    }
}
