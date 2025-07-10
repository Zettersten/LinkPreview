namespace LinkPreview.Polyfills.Squidlr.LinkedIn;

public sealed class LinkedInWebClient : PlatformWebClient
{
    public const string HttpClientName = nameof(LinkedInWebClient);

    public LinkedInWebClient(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory, HttpClientName) { }

    public Task<HttpResponseMessage> GetLinkedInPostAsync(
        LinkedInIdentifier identifier,
        CancellationToken cancellationToken
    )
    {
        var client = this.CreateClient();
        return client.GetAsync(identifier.Url, cancellationToken);
    }
}
