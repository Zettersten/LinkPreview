namespace LinkPreview.Polyfills.Squidlr;

public readonly struct VideoSize : IEquatable<VideoSize>
{
    public readonly int Height { get; init; }

    public readonly int Width { get; init; }

    public static VideoSize Empty => new() { Height = 0, Width = 0 };

    public VideoSize(int height, int width)
    {
        this.Height = height;
        this.Width = width;
    }

    public override bool Equals(object? obj)
    {
        return obj is VideoSize size && this.Equals(size);
    }

    public bool Equals(VideoSize other)
    {
        return this.Height == other.Height && this.Width == other.Width;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.Height, this.Width);
    }

    public static bool operator ==(VideoSize left, VideoSize right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(VideoSize left, VideoSize right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return $"{this.Width}x{this.Height}";
    }
}
