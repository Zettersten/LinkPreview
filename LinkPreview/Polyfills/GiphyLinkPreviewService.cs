using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace LinkPreview.Polyfills;

public sealed partial class GiphyLinkPreviewService(IHttpClientFactory httpClientFactory)
    : PolyfillBase,
        ILinkPreviewPolyfill
{
    private const string iconHref = "https://giphy.com/static/img/icons/apple-touch-icon-120px.png";
    private const int iconWidth = 120;
    private const int iconHeight = 120;

    /// <summary>
    /// sourced from the internet
    /// </summary>
    private static readonly Queue<string> rotatingApiKeys =
        new(
            [
                "Ibyf64xoQQo6sUoTg2QbxMQI4MG5zrR4",
                "O4FTCFzVGQFAhaCzS9uFD5NFBY8tt14I",
                "YjKm2LCQbRa7vCx1qYsBiORB5uWabGrH",
                "qeGiBHwYSk7B6lIuL9dQy9EBZ6ZSZEgs",
                "yTbYS3FsMkCfiiofE4FTJAhf0zpELxYK",
                "YqLensbIWv5skyGVSr6ZPFClfQImMmX4",
                "zd5rWKKeQu7wqyr9Bb5nec4cc7PWmK2S",
                "rs40lGAaQGDam6kxgXlV6ZAR4srLUYVh",
                "EjzvMRueNdiAkT3CvCjx0kOjl8qGzxLM",
                "23DdXlqxN1Jjk8o8wO7TLcPhm4Uxv2e1",
                "3cqcb8LEg33MtM0vWp2nMTE6iMswMXML"
            ]
        );

    public bool IsEager => true;

    public int Order => 1;

    public bool CanHandle(string url)
    {
        return !string.IsNullOrWhiteSpace(url)
            && (
                url.Contains("giphy.com/", StringComparison.OrdinalIgnoreCase)
                || url.Contains("gph.is/", StringComparison.OrdinalIgnoreCase)
            )
            && Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    public async Task<LinkPreviewResponse?> TryGetLinkPreviewAsync(
        string url,
        CancellationToken cancellationToken
    )
    {
        var imageId = ExtractImageIdFromUrl(url);

        if (string.IsNullOrWhiteSpace(imageId))
        {
            return null;
        }

        var httpClient = httpClientFactory.CreateClient(nameof(GiphyLinkPreviewService));
        var maxRetries = rotatingApiKeys.Count;
        var currentRetry = 0;
        var jsonString = string.Empty;

        while (true)
        {
            var apiKey = GetNextApiKey();
            var route = $"https://api.giphy.com/v1/gifs/{imageId}?api_key={apiKey}";
            var request = new HttpRequestMessage(HttpMethod.Get, route);

            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
                    currentRetry++;
                    continue; // Retry after delay
                }

                return null;
            }

            jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
            break;
        }

        if (string.IsNullOrWhiteSpace(jsonString))
        {
            return null;
        }

        var jsonObject = JsonSerializer.Deserialize<JsonObject>(jsonString);

        if (jsonObject is null)
        {
            return null;
        }

        if (
            jsonObject["data"] == null
            || jsonObject["data"]?["title"] == null
            || jsonObject["data"]?["username"] == null
            || jsonObject["data"]?["url"] == null
            || jsonObject["data"]?["images"] == null
            || jsonObject["data"]?["images"]?["original"] == null
            || jsonObject["data"]?["images"]?["original"]?["url"] == null
        )
        {
            return null;
        }

        var description = jsonObject["data"]?["title"]?.GetValue<string>() ?? "Giphy GIF";
        var username = jsonObject["data"]?["username"]?.GetValue<string>() ?? "Anonymous";
        var pageUrl = jsonObject["data"]?["url"]?.GetValue<string>() ?? url;
        var imageUrl = jsonObject["data"]?["images"]?["original"]?["url"]?.GetValue<string>()!;
        var imageHeight = int.Parse(
            jsonObject["data"]?["images"]?["original"]?["height"]?.GetValue<string>() ?? "0"
        );
        var imageWidth = int.Parse(
            jsonObject["data"]?["images"]?["original"]?["width"]?.GetValue<string>() ?? "0"
        );
        var title = $"A GIF created by {username}";

        return new LinkPreviewResponse
        {
            Title = title,
            Description = description,
            Url = pageUrl,
            Icon = iconHref,
            IconHeight = iconHeight,
            IconWidth = iconWidth,
            ImageType = "image/gif",
            Image = imageUrl,
            ImageHeight = imageHeight,
            ImageWidth = imageWidth,
            IconType = "image/png",
        };
    }

    private static string GetNextApiKey()
    {
        lock (rotatingApiKeys)
        {
            if (rotatingApiKeys.Count == 0)
            {
                throw new InvalidOperationException("No Giphy API keys available.");
            }

            var key = rotatingApiKeys.Dequeue();
            rotatingApiKeys.Enqueue(key);
            return key;
        }
    }

    private static string? ExtractImageIdFromUrl(string url) =>
        string.IsNullOrWhiteSpace(url)
            ? null
            : giphyPatterns
                .Select(pattern => pattern.Match(url))
                .FirstOrDefault(match => match.Success)
                ?.Groups[1]
                .Value;

    private static readonly Regex[] giphyPatterns =
    [
        // Standard giphy.com URLs: https://giphy.com/gifs/title-words-ID
        DefaultPattern(),
        // Giphy.com embed URLs: https://giphy.com/embed/ID
        EmbedRegexPattern(),
        // Media URLs: https://media.giphy.com/media/.../ID/giphy.gif
        MediaRegexPattern(),
        // i.giphy.com URLs: https://i.giphy.com/ID.gif
        ShortCodeRegexPattern(),
        // Shortened gph.is URLs: https://gph.is/ID
        ShareRegexPattern(),
        // Alternative giphy.com format: https://giphy.com/gifs/ID
        AltPattern()
    ];

    [GeneratedRegex(
        @"(?:https?://)?(?:www\.)?giphy\.com/embed/([a-zA-Z0-9]+)/?",
        RegexOptions.Compiled
    )]
    private static partial Regex EmbedRegexPattern();

    [GeneratedRegex(
        @"(?:https?://)?media\.giphy\.com/media/[^/]+/([a-zA-Z0-9]+)/",
        RegexOptions.Compiled
    )]
    private static partial Regex MediaRegexPattern();

    [GeneratedRegex(@"(?:https?://)?i\.giphy\.com/([a-zA-Z0-9]+)\.gif", RegexOptions.Compiled)]
    private static partial Regex ShortCodeRegexPattern();

    [GeneratedRegex(@"(?:https?://)?gph\.is/([a-zA-Z0-9]+)/?", RegexOptions.Compiled)]
    private static partial Regex ShareRegexPattern();

    [GeneratedRegex(
        @"(?:https?://)?(?:www\.)?giphy\.com/gifs/([a-zA-Z0-9]+)/?$",
        RegexOptions.Compiled
    )]
    private static partial Regex AltPattern();

    [GeneratedRegex(
        @"(?:https?://)?(?:www\.)?giphy\.com/gifs/[^/]*-([a-zA-Z0-9]+)/?",
        RegexOptions.Compiled
    )]
    private static partial Regex DefaultPattern();
}
