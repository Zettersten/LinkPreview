using ImageSizeReader;

namespace LinkPreview.Polyfills;

public partial class WhatNotLinkPreviewService : PolyfillBase, ILinkPreviewPolyfill
{
    private readonly IImageSizeReaderUtil imageUtils;
    private readonly HttpClient httpClient;

    public WhatNotLinkPreviewService(IImageSizeReaderUtil imageSizeReaderUtil)
    {
        this.httpClient = new HttpClient()
        {
            DefaultRequestVersion = new Version(2, 0) // Enforce HTTP/2
        };

        this.imageUtils = imageSizeReaderUtil;
    }

    public bool IsEager => true;

    public int Order => 0;

    private static bool IsValidLink(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            return false;
        }

        if (!url.StartsWith("https://www.whatnot.com/") && !url.StartsWith("https://whatnot.com/"))
        {
            return false;
        }

        return true;
    }

    private static HttpRequestMessage CreateWhatNotHtmlRequestMessage(string url, string userAgent)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Version = new Version(2, 0) // Enforce HTTP/2
        };

        if (url.Contains("images.whatnot", StringComparison.OrdinalIgnoreCase))
        {
            requestMessage.Headers.Host = "images.whatnot.com";
        }
        else
        {
            requestMessage.Headers.Host = "www.whatnot.com";
        }

        requestMessage.Headers.Add("pragma", "no-cache");
        requestMessage.Headers.Add("cache-control", "no-cache");
        requestMessage.Headers.Add(
            "sec-ch-ua",
            "\"Microsoft Edge\";v=\"137\", \"Chromium\";v=\"137\", \"Not/A)Brand\";v=\"24\""
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
        requestMessage.Headers.Add("sec-fetch-site", "same-origin");
        requestMessage.Headers.Add("sec-fetch-mode", "navigate");
        requestMessage.Headers.Add("sec-fetch-user", "?1");
        requestMessage.Headers.Add("sec-fetch-dest", "document");
        //requestMessage.Headers.Add("accept-encoding", "gzip, deflate, br, zstd");
        requestMessage.Headers.Add("accept-language", "en-US,en;q=0.9");
        requestMessage.Headers.Add("priority", "u=0, i");

        return requestMessage;
    }

    public static LinkPreviewResponse ParseWhatNotResponse(string url, string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            throw new ArgumentException(
                "HTML content cannot be null or empty.",
                nameof(htmlContent)
            );
        }

        var metadata = RegexUtilities.GetMetadata(htmlContent, "https://www.whatnot.com/");

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

        return new LinkPreviewResponse
        {
            Title = metadata.TwitterTitle ?? metadata.OgTitle ?? metadata.SiteTitle ?? string.Empty,
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

    public bool CanHandle(string url) => IsValidLink(url);

    public async Task<LinkPreviewResponse?> TryGetLinkPreviewAsync(
        string url,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (!IsValidLink(url))
            {
                throw new LinkPreviewException(
                    System.Net.HttpStatusCode.InternalServerError,
                    "URL provided was not WhatNot."
                );
            }

            var response = await this.SendWithRedirectAsync(
                this.httpClient,
                CreateWhatNotHtmlRequestMessage,
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
                    "WhatNot response was empty."
                );
            }

            var linkPreviewResponse = ParseWhatNotResponse(url, htmlContent);

            var (ImageHeight, ImageWidth, ImageBytes) = await this.DownloadAndExtractImageAsync(
                linkPreviewResponse.Image,
                this.httpClient,
                this.imageUtils,
                CreateWhatNotHtmlRequestMessage,
                cancellationToken
            );

            linkPreviewResponse.ImageHeight = ImageHeight;
            linkPreviewResponse.ImageWidth = ImageWidth;
            linkPreviewResponse.ImageType = Convert.ToBase64String(ImageBytes);

            return linkPreviewResponse;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
