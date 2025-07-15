using System.Text.Json;
using System.Text.Json.Nodes;
using ImageSizeReader;

namespace LinkPreview.Polyfills;

public sealed class TwitterRapidApiLinkPreviewService(
    IHttpClientFactory httpClientFactory,
    IImageSizeReaderUtil imageSizeReaderUtil
) : PolyfillBase, ILinkPreviewPolyfill
{
    public bool IsEager => true;

    public int Order => 2;

    public bool CanHandle(string url)
    {
        return !string.IsNullOrWhiteSpace(url)
            && (
                url.Contains("twitter.com/", StringComparison.OrdinalIgnoreCase)
                || url.Contains("twimg.com/", StringComparison.OrdinalIgnoreCase)
                || url.Contains("x.com/", StringComparison.OrdinalIgnoreCase)
            )
            && Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    public async Task<LinkPreviewResponse?> TryGetLinkPreviewAsync(
        string url,
        CancellationToken cancellationToken
    )
    {
        var httpClient = httpClientFactory.CreateClient(nameof(TwitterRapidApiLinkPreviewService));

        var json = string.Empty;

        while (true)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://twitter241.p.rapidapi.com/tweet-v2?pid={url}"),
                Headers =
                {
                    { "x-rapidapi-key", "6a567b6e54msh6627e836345c6ccp1020ccjsnc928e7fb2f00" },
                },
            };

            var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    continue; // Retry after delay
                }

                return null;
            }

            json = await response.Content.ReadAsStringAsync(cancellationToken);

            break;
        }

        var result = ParseJson(url, json);

        if (result is null or { Image: null })
        {
            return null;
        }

        var imageContent = await this.GetTwitterPostImageByUrl(result.Image, cancellationToken);

        if (imageContent is not null)
        {
            var dimensions = imageSizeReaderUtil.GetDimensions(imageContent);
            result.IconHeight = dimensions?.Height ?? result.IconHeight;
            result.IconWidth = dimensions?.Width ?? result.IconWidth;

            if (imageContent.CanSeek)
            {
                imageContent.Seek(0, SeekOrigin.Begin);
            }

            result.ImageType = Convert.ToBase64String(imageContent.ToArray());
        }

        return result;
    }

    private static LinkPreviewResponse? ParseJson(string url, string json)
    {
        try
        {
            var jsonObject = JsonSerializer.Deserialize<JsonObject>(json);

            if (
                jsonObject is null
                || jsonObject["result"] is null
                || jsonObject["result"]?["tweetResult"] is null
                || jsonObject["result"]?["tweetResult"]?["result"] is null
                || jsonObject["result"]?["tweetResult"]?["result"]?["core"] is null
                || jsonObject["result"]?["tweetResult"]?["result"]?["core"]?["user_results"] is null
            )
            {
                return null;
            }

            var iconHref = "https://abs.twimg.com/responsive-web/client-web/icon-ios.77d25eba.png";
            var iconWidth = 1024;
            var iconHeight = 1024;
            var fullName = jsonObject["result"]
                ?["tweetResult"]?["result"]?["core"]?["user_results"]?["result"]?["legacy"]?[
                    "name"
                ]?.GetValue<string>();
            var screenName = jsonObject["result"]
                ?["tweetResult"]?["result"]?["core"]?["user_results"]?["result"]?["legacy"]?[
                    "screen_name"
                ]?.GetValue<string>()!;
            var title = $"A post shared by {fullName} (@{screenName})";

            return new LinkPreviewResponse
            {
                Icon = iconHref,
                IconWidth = iconWidth,
                IconHeight = iconHeight,
                Title = title,
                Image = jsonObject["result"]
                    ?["tweetResult"]?["result"]?["legacy"]?["entities"]?["media"]?.AsArray()
                    .FirstOrDefault()
                    ?["media_url_https"]?.GetValue<string>()!,
                Description =
                    jsonObject["result"]
                        ?["tweetResult"]?["result"]?["legacy"]?["full_text"]?.GetValue<string>()
                    ?? string.Empty,
                Url = url,
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<MemoryStream?> GetTwitterPostImageByUrl(
        string url,
        CancellationToken cancellationToken = default
    )
    {
        url = ExtractPostId(url);

        var post = await this.TryGetLinkPreviewAsync(url, cancellationToken);

        if (post is null || string.IsNullOrWhiteSpace(post.Image))
        {
            return null;
        }

        var httpClient = httpClientFactory.CreateClient(nameof(TwitterRapidApiLinkPreviewService));

        var response = await httpClient.GetAsync(post.Image, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        ms.Seek(0, SeekOrigin.Begin); // Reset position for reading
        await stream.DisposeAsync(); // Dispose the original response stream to free resources
        return ms;
    }

    /// <summary>
    /// Removes the username segment from twitter URLs if present
    /// </summary>
    public static string ExtractPostId(string url) => url.Split('/').Last().Trim();
}
