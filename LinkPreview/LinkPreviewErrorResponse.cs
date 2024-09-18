using System.Text.Json.Serialization;

namespace LinkPreview;

/// <summary>
/// Represents the error response model for the LinkPreview service.
/// </summary>
public sealed class LinkPreviewErrorResponse
{
    /// <summary>
    /// Gets or sets the title of the error response.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the error.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the image URL associated with the error, if any.
    /// </summary>
    [JsonPropertyName("image")]
    public string Image { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL associated with the error, if any.
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    [JsonPropertyName("error")]
    public int Error { get; set; }
}