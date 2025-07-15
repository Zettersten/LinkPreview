using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ImageSizeReader;

namespace LinkPreview.Polyfills;

public sealed partial class InstagramRapidApiLinkPreviewService(
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
                url.Contains("instagram.com/", StringComparison.OrdinalIgnoreCase)
                || url.Contains("instagr.am/", StringComparison.OrdinalIgnoreCase)
                || url.Contains("instagr.com/", StringComparison.OrdinalIgnoreCase)
                || url.Contains("ig.me/", StringComparison.OrdinalIgnoreCase)
                || url.Contains("ig.com/", StringComparison.OrdinalIgnoreCase)
            )
            && Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    public async Task<LinkPreviewResponse?> TryGetLinkPreviewAsync(
        string url,
        CancellationToken cancellationToken
    )
    {
        var httpClient = httpClientFactory.CreateClient(
            nameof(InstagramRapidApiLinkPreviewService)
        );

        var json = string.Empty;
        var totalRetryAttempts = 0;

        while (true)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(
                    $"https://instagram-scraper-stable-api.p.rapidapi.com/get_media_data.php?reel_post_code_or_url={url}&type=post"
                ),
                Headers =
                {
                    { "x-rapidapi-key", "6a567b6e54msh6627e836345c6ccp1020ccjsnc928e7fb2f00" },
                },
            };

            var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (totalRetryAttempts > 3)
                {
                    break;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

                    totalRetryAttempts++;

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

        var imageContent = await this.GetInstagramPostImageByUrl(result.Image, cancellationToken);

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
                || jsonObject["id"] is null
                || jsonObject["display_url"] is null
                || jsonObject["owner"] is null
                || jsonObject["edge_media_to_caption"] is null
            )
            {
                return null;
            }

            var displayName = jsonObject["owner"]?["full_name"]?.GetValue<string>()!;
            var username = jsonObject["owner"]?["username"]?.GetValue<string>()!;
            var title = $"A post shared by {displayName} (@{username})";

            var linkPreviewResponse = new LinkPreviewResponse
            {
                Title = title,
                Icon =
                    "https://www.instagram.com/static/images/ico/apple-touch-icon-180x180-precomposed.png/c06fdb2357bd.png",
                IconHeight = 180,
                IconWidth = 180,
                Image = jsonObject["display_url"]?.GetValue<string>()!,
                Description =
                    jsonObject["edge_media_to_caption"]
                        ?["edges"]?.AsArray()
                        ?.FirstOrDefault()
                        ?["node"]?["text"]?.GetValue<string>() ?? string.Empty,
                Url = url
            };

            return linkPreviewResponse;
        }
        catch
        {
            return null;
        }
    }

    public async Task<MemoryStream?> GetInstagramPostImageByUrl(
        string url,
        CancellationToken cancellationToken = default
    )
    {
        var httpClient = httpClientFactory.CreateClient(
            nameof(InstagramRapidApiLinkPreviewService)
        );

        var response = await httpClient.GetAsync(url, cancellationToken);

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
    /// Removes the username segment from Instagram URLs if present
    /// </summary>
    public static string RemoveInstagramUsername(string url) =>
        UsernameRegexPattern()
            .Replace(url, "$1$2")
            .Replace("//p", "/p")
            .Replace("//reel", "/reel")
            .Trim();

    [GeneratedRegex(@"(https://www\.instagram\.com/)[^/]+(/(?:p|reel)/)")]
    private static partial Regex UsernameRegexPattern();
}
