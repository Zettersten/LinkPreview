namespace LinkPreview.Polyfills.Squidlr;

public abstract class PlatformWebClient
{
    private readonly string httpClientName;
    private readonly IHttpClientFactory clientFactory;

    protected PlatformWebClient(IHttpClientFactory httpClientFactory, string httpClientName)
    {
        if (string.IsNullOrEmpty(httpClientName))
        {
            throw new ArgumentException(
                $"'{nameof(httpClientName)}' cannot be null or empty.",
                nameof(httpClientName)
            );
        }

        this.httpClientName = httpClientName;
        this.clientFactory =
            httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    protected HttpClient CreateClient() => this.CreateClient(this.httpClientName);

    protected HttpClient CreateClient(string httpClientName)
    {
        if (string.IsNullOrEmpty(httpClientName))
        {
            throw new ArgumentException(
                $"'{nameof(httpClientName)}' cannot be null or empty.",
                nameof(httpClientName)
            );
        }

        return this.clientFactory.CreateClient(httpClientName);
    }

    public async ValueTask<(long?, string?)> GetVideoContentLengthAndMediaTypeAsync(
        Uri videoFileUri,
        CancellationToken cancellationToken
    )
    {
        var client = this.CreateClient();
        var requestMessage = new HttpRequestMessage(HttpMethod.Head, videoFileUri);
        using var response = await client.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );

        if (response.IsSuccessStatusCode)
        {
            return (
                response.Content.Headers.ContentLength,
                response.Content.Headers.ContentType?.MediaType
            );
        }

        return (null, null);
    }
}
