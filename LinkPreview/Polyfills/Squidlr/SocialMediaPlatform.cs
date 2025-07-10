using System.Text.Json.Serialization;

namespace LinkPreview.Polyfills.Squidlr;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SocialMediaPlatform
{
    Unknown,

    Twitter,

    Instagram,

    Tiktok,

    LinkedIn,

    Facebook
}
