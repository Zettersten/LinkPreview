using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LinkPreview;

/// <summary>
/// Represents the response model for the LinkPreview service, covering both Default and Extended responses.
/// </summary>
public sealed class LinkPreviewResponse
{
    /// <summary>
    /// Gets or sets the website title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description summary.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the preview image URL.
    /// </summary>
    [JsonPropertyName("image")]
    [Url]
    public string Image { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination URL.
    /// </summary>
    [JsonPropertyName("url")]
    [Url]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the image size in bytes.
    /// </summary>
    [JsonPropertyName("image_size")]
    public long? ImageSize { get; set; }

    /// <summary>
    /// Gets or sets the image MIME content type.
    /// </summary>
    [JsonPropertyName("image_type")]
    public string? ImageType { get; set; }

    /// <summary>
    /// Gets or sets the image width in pixels.
    /// </summary>
    [JsonPropertyName("image_x")]
    public int? ImageWidth { get; set; }

    /// <summary>
    /// Gets or sets the image height in pixels.
    /// </summary>
    [JsonPropertyName("image_y")]
    public int? ImageHeight { get; set; }

    /// <summary>
    /// Gets or sets the website icon URL (favicon).
    /// </summary>
    [JsonPropertyName("icon")]
    [Url]
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets the website icon MIME content type.
    /// </summary>
    [JsonPropertyName("icon_type")]
    public string? IconType { get; set; }

    /// <summary>
    /// Gets or sets the website icon width in pixels.
    /// </summary>
    [JsonPropertyName("icon_x")]
    public int? IconWidth { get; set; }

    /// <summary>
    /// Gets or sets the website icon height in pixels.
    /// </summary>
    [JsonPropertyName("icon_y")]
    public int? IconHeight { get; set; }

    /// <summary>
    /// Gets or sets the web page's locale formatted as language_TERRITORY.
    /// </summary>
    [JsonPropertyName("locale")]
    [RegularExpression(@"^[a-z]{2}_[A-Z]{2}$", ErrorMessage = "Locale must be in the format language_TERRITORY (e.g., en_US)")]
    public string? Locale { get; set; }

    /// <summary>
    /// Determines whether the response includes extended properties.
    /// </summary>
    /// <returns>True if the response includes any extended properties; otherwise, false.</returns>
    public bool IsExtendedResponse()
    {
        return ImageSize.HasValue ||
               !string.IsNullOrEmpty(ImageType) ||
               ImageWidth.HasValue ||
               ImageHeight.HasValue ||
               !string.IsNullOrEmpty(Icon) ||
               !string.IsNullOrEmpty(IconType) ||
               IconWidth.HasValue ||
               IconHeight.HasValue ||
               !string.IsNullOrEmpty(Locale);
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        return $"Title: {Title}, URL: {Url}, Is Extended Response: {IsExtendedResponse()}";
    }
}