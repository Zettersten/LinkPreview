using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace LinkPreview;

/// <summary>
/// Implementation of the LinkPreview service.
/// </summary>
public sealed class LinkPreviewService : ILinkPreviewService
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<LinkPreviewOptions> _options;
    private readonly IMemoryCache _cache;

    public LinkPreviewService(HttpClient httpClient, IOptions<LinkPreviewOptions> options, IMemoryCache cache)
    {
        _httpClient = httpClient ?? throw new LinkPreviewException("HTTP client is null.");
        _options = options ?? throw new LinkPreviewException("LinkPreview options are null.");
        _cache = cache ?? throw new LinkPreviewException("Memory cache is null.");

        try
        {
            _options.Value.Validate();
        }
        catch (ValidationException ex)
        {
            throw new LinkPreviewException($"Invalid LinkPreview options: {ex.Message}");
        }

        _httpClient.BaseAddress = new Uri(_options.Value.ApiBaseUrl);
        _httpClient.DefaultRequestHeaders.Add("X-Linkpreview-Api-Key", _options.Value.ApiKey);
    }

    /// <inheritdoc />
    public async Task<LinkPreviewResponse> GetLinkPreviewAsync(string url, LinkPreviewOptionalField? optionalFields = null, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(url, optionalFields);

        if (_cache.TryGetValue(cacheKey, out LinkPreviewResponse? cachedResponse))
        {
            return cachedResponse!;
        }

        var response = await FetchLinkPreviewAsync(url, optionalFields, cancellationToken);

        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(_options.Value.CacheTTLMinutes));

        _cache.Set(cacheKey, response, cacheEntryOptions);

        return response;
    }

    private async Task<LinkPreviewResponse> FetchLinkPreviewAsync(string url, LinkPreviewOptionalField? optionalFields, CancellationToken cancellationToken)
    {
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                throw new LinkPreviewException(System.Net.HttpStatusCode.BadRequest, "The URL is not valid.");
            }

            var queryParams = new Dictionary<string, string?>
            {
                { "q", Uri.EscapeDataString(url) }
            };

            var fields = GetFieldsFromEnum(optionalFields);

            if (!string.IsNullOrEmpty(fields))
            {
                queryParams.Add("fields", fields);
            }

            var response = await _httpClient.GetAsync(QueryHelpers.AddQueryString("", queryParams), cancellationToken);

            response.EnsureSuccessStatusCode();

            var linkPreviewResponseString = await response.Content.ReadAsStringAsync(cancellationToken);

            var linkPreviewResponse = JsonSerializer.Deserialize<LinkPreviewResponse>(linkPreviewResponseString);

            return linkPreviewResponse ?? throw new LinkPreviewException(System.Net.HttpStatusCode.InternalServerError, "Failed to deserialize the API response.");
        }
        catch (HttpRequestException ex)
        {
            throw new LinkPreviewException(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError, ex.Message);
        }
        catch (TaskCanceledException)
        {
            throw new LinkPreviewException(System.Net.HttpStatusCode.RequestTimeout, "The request timed out.");
        }
        catch (Exception ex) when (ex is not LinkPreviewException)
        {
            throw new LinkPreviewException($"An unexpected error occurred: {ex.Message}");
        }
    }

    private static string GetCacheKey(string url, LinkPreviewOptionalField? optionalFields)
    {
        return $"LinkPreview_{url}_{optionalFields}";
    }

    private static string GetFieldsFromEnum(LinkPreviewOptionalField? fields = null)
    {
        if (fields == null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        if (fields.Value.HasFlag(LinkPreviewOptionalField.Canonical)) AppendField(sb, "canonical");
        if (fields.Value.HasFlag(LinkPreviewOptionalField.Locale)) AppendField(sb, "locale");
        if (fields.Value.HasFlag(LinkPreviewOptionalField.SiteName)) AppendField(sb, "site_name");
        if (fields.Value.HasFlag(LinkPreviewOptionalField.ImageX)) AppendField(sb, "image_x");
        if (fields.Value.HasFlag(LinkPreviewOptionalField.ImageY)) AppendField(sb, "image_y");
        if (fields.Value.HasFlag(LinkPreviewOptionalField.ImageSize)) AppendField(sb, "image_size");
        if (fields.Value.HasFlag(LinkPreviewOptionalField.ImageType)) AppendField(sb, "image_type");
        if (fields.Value.HasFlag(LinkPreviewOptionalField.Icon)) AppendField(sb, "icon");
        if (fields.Value.HasFlag(LinkPreviewOptionalField.IconX)) AppendField(sb, "icon_x");
        if (fields.Value.HasFlag(LinkPreviewOptionalField.IconY)) AppendField(sb, "icon_y");
        if (fields.Value.HasFlag(LinkPreviewOptionalField.IconSize)) AppendField(sb, "icon_size");
        if (fields.Value.HasFlag(LinkPreviewOptionalField.IconType)) AppendField(sb, "icon_type");

        return sb.ToString().TrimEnd(',');
    }

    private static void AppendField(StringBuilder sb, string field)
    {
        sb.Append(field).Append(',');
    }
}