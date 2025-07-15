using ImageSizeReader;

namespace LinkPreview.Polyfills;

/// <summary>
/// Base class for all link preview polyfills, providing user agent rotation and metadata extraction.
/// </summary>
public abstract partial class PolyfillBase
{
    private readonly object lockObject = new();
    private string? lastUsedUserAgent;
    private int currentUsageCount;

    private readonly Queue<string> userAgentQueue = RegexUtilities.CreateUserAgentQueue(
        botAgentsOnly: false
    );

    /// <summary>
    /// Rotates through a set of user agents, using each for up to 5 requests.
    /// </summary>
    protected string GetNextUserAgent()
    {
        lock (this.lockObject)
        {
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

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            var retryCount = 0;
            var totalRetries = this.userAgentQueue.Count;

            // Retry with a new user agent if forbidden
            while (
                response.StatusCode == System.Net.HttpStatusCode.Forbidden
                && retryCount < totalRetries
            )
            {
                ua = this.GetNextUserAgent();
                request = createRequest(url, ua);
                response = await client.SendAsync(request, cancellationToken);
                retryCount++;
            }
        }

        return response;
    }

    protected async Task<(int Height, int Width, byte[] Bytes)> DownloadAndExtractImageAsync(
        string imageUrl,
        HttpClient httpClient,
        IImageSizeReaderUtil imageUtils,
        Func<string, string, HttpRequestMessage> createRequest,
        CancellationToken cancellationToken
    )
    {
        var userAgent = this.GetNextUserAgent();
        var imageRequest = createRequest(imageUrl, userAgent);

        using var response = await httpClient.SendAsync(imageRequest, cancellationToken);

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

        if (imageContent.CanSeek)
        {
            imageContent.Seek(0, SeekOrigin.Begin);
        }

        if (imageContent.Length == 0)
        {
            throw new LinkPreviewException(
                System.Net.HttpStatusCode.InternalServerError,
                "Image response was empty."
            );
        }

        var dimensions = imageUtils.GetDimensions(imageContent);

        if (dimensions != null)
        {
            if (ms.CanSeek)
            {
                ms.Seek(0, SeekOrigin.Begin);
            }

            return (dimensions.Height, dimensions.Width, ms.ToArray());
        }

        throw new LinkPreviewException(
            System.Net.HttpStatusCode.InternalServerError,
            "Unsupported image format."
        );
    }
}
