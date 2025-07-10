namespace LinkPreview.Polyfills.Squidlr;

public struct ContentIdentifier : IEquatable<ContentIdentifier>
{
    public ContentIdentifier(SocialMediaPlatform platform, string id, string url)
    {
        ArgumentNullException.ThrowIfNull(url);
        this.Platform = platform;
        this.Id = id;
        this.Url = url;
    }

    public SocialMediaPlatform Platform { get; set; }

    public string Id { get; set; }

    public string Url { get; set; }

    public override readonly bool Equals(object? obj)
    {
        return obj is ContentIdentifier identifier && this.Equals(identifier);
    }

    public readonly bool Equals(ContentIdentifier other)
    {
        return this.Id == other.Id;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(this.Id);
    }

    public static bool operator ==(ContentIdentifier left, ContentIdentifier right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ContentIdentifier left, ContentIdentifier right)
    {
        return !(left == right);
    }

    public override readonly string? ToString()
    {
        if (!string.IsNullOrEmpty(this.Id) && !string.IsNullOrEmpty(this.Url))
        {
            return $"ID: {this.Id} Url: {this.Url}";
        }

        return base.ToString();
    }

    public static readonly ContentIdentifier Unknown =
        new(SocialMediaPlatform.Unknown, string.Empty, string.Empty);
}
