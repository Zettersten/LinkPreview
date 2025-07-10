using System.Text.Json.Serialization;

namespace LinkPreview.Polyfills.Squidlr;

public abstract class Content
{
    public string SourceUrl { get; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SocialMediaPlatform Platform { get; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public string? Username { get; set; }

    public string? FullText { get; set; }

    public int FavoriteCount { get; set; }

    public int ReplyCount { get; set; }

    public VideoCollection Videos { get; set; } = [];

    [JsonIgnore]
    public Dictionary<string, string> AdditionalProperties { get; set; } = new();

    protected Content(string sourceUrl, SocialMediaPlatform platform)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceUrl);
        this.SourceUrl = sourceUrl;
        this.Platform = platform;
    }

    public void AddVideo(Video video)
    {
        ArgumentNullException.ThrowIfNull(video);
        this.Videos.Add(video);
    }

    public virtual string GetSafeVideoFileName(VideoSource video)
    {
        if (!string.IsNullOrEmpty(this.Username))
        {
            return $"{this.Platform.GetPlatformName()}-{this.Username}-{Path.GetFileName(video.Url.AbsolutePath)}";
        }

        return $"{this.Platform.GetPlatformName()}-{Path.GetFileName(video.Url.AbsolutePath)}";
    }
}
