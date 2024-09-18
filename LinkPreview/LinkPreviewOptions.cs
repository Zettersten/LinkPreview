using System.ComponentModel.DataAnnotations;

namespace LinkPreview;

/// <summary>
/// Represents the configuration options for the LinkPreview SDK.
/// </summary>
public sealed class LinkPreviewOptions
{
    private const string defaultApiBaseUrl = "https://api.linkpreview.net";

    /// <summary>
    /// Gets or sets the base URL for the LinkPreview API.
    /// </summary>
    [Url]
    [Required(ErrorMessage = "API Base URL is required")]
    public string ApiBaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the API key for authenticating with the LinkPreview service.
    /// </summary>
    [Required(ErrorMessage = "API Key is required")]
    public string ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the cache Time To Live (TTL) in minutes.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Cache TTL must be a positive number")]
    public int CacheTTLMinutes { get; set; } = 60; // Default to 1 hour

    /// <summary>
    /// Initializes a new instance of the <see cref="LinkPreviewOptions"/> class with default values.
    /// </summary>
    public LinkPreviewOptions()
    {
        this.ApiBaseUrl = defaultApiBaseUrl;
        this.ApiKey = string.Empty;
    }

    /// <summary>
    /// Validates the current options.
    /// </summary>
    /// <exception cref="ValidationException">Thrown when the options are invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(this.ApiKey))
        {
            throw new ValidationException("API Key must not be empty or whitespace.");
        }

        if (!Uri.TryCreate(this.ApiBaseUrl, UriKind.Absolute, out _))
        {
            throw new ValidationException("API Base URL must be a valid absolute URL.");
        }
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        return $"API Base URL: {this.ApiBaseUrl}, API Key: {this.ApiKey[..3]}...{this.ApiKey[^3..]}";
    }
}
