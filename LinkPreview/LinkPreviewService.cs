using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using LinkPreview.Polyfills;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LinkPreview;

/// <summary>
/// Implementation of the LinkPreview service.
/// </summary>
public sealed class LinkPreviewService : ILinkPreviewService
{
    private readonly HttpClient httpClient;
    private readonly IOptions<LinkPreviewOptions> options;
    private readonly IMemoryCache cache;
    private IEnumerable<ILinkPreviewPolyfill> polyfills;

    public LinkPreviewService(
        HttpClient httpClient,
        IOptions<LinkPreviewOptions> options,
        IMemoryCache cache,
        IEnumerable<ILinkPreviewPolyfill> polyfills
    )
    {
        this.httpClient = httpClient;
        this.options = options;
        this.cache = cache;

        try
        {
            this.options.Value.Validate();
        }
        catch (ValidationException ex)
        {
            throw new LinkPreviewException($"Invalid LinkPreview options: {ex.Message}");
        }

        this.httpClient.BaseAddress = new Uri(this.options.Value.ApiBaseUrl);
        this.httpClient.DefaultRequestHeaders.Add(
            "X-Linkpreview-Api-Key",
            this.options.Value.ApiKey
        );

        this.polyfills = polyfills;
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("")]
    [RequiresDynamicCode("")]
    public async Task<LinkPreviewResponse> GetLinkPreviewAsync(
        string url,
        LinkPreviewOptionalField? optionalFields = null,
        CancellationToken cancellationToken = default
    )
    {
        var cacheKey = GetCacheKey(url, optionalFields);

        if (this.cache.TryGetValue(cacheKey, out LinkPreviewResponse? cachedResponse))
        {
            return cachedResponse!;
        }

        var response = await this.FetchLinkPreviewAsync(url, optionalFields, cancellationToken);

        var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(
            TimeSpan.FromMinutes(this.options.Value.CacheTTLMinutes)
        );

        this.cache.Set(cacheKey, response, cacheEntryOptions);

        return response;
    }

    [RequiresUnreferencedCode(
        "Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)"
    )]
    [RequiresDynamicCode(
        "Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)"
    )]
    private async Task<LinkPreviewResponse> FetchLinkPreviewAsync(
        string url,
        LinkPreviewOptionalField? optionalFields,
        CancellationToken cancellationToken
    )
    {
        // 1. Eager polyfills
        foreach (var polyfill in this.polyfills.Where(p => p.IsEager && p.CanHandle(url)))
        {
            var eagerResult = await polyfill.TryGetLinkPreviewAsync(url, cancellationToken);

            if (eagerResult != null)
            {
                eagerResult.IsPolyfill = true;
                return eagerResult;
            }
        }

        // 2. Main API
        LinkPreviewResponse? linkPreviewResponse = null;
        Exception? mainApiException = null;

        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                throw new LinkPreviewException(
                    System.Net.HttpStatusCode.BadRequest,
                    "The URL is not valid."
                );
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

            var response = await this.httpClient.GetAsync(
                QueryHelpers.AddQueryString("", queryParams),
                cancellationToken
            );

            if (
                response.StatusCode != System.Net.HttpStatusCode.OK
                && response.StatusCode != (System.Net.HttpStatusCode)425
            )
            {
                throw new LinkPreviewException("Failed to make the API request.");
            }

            var linkPreviewResponseString = await response.Content.ReadAsStringAsync(
                cancellationToken
            );

            linkPreviewResponse = JsonSerializer.Deserialize<LinkPreviewResponse>(
                linkPreviewResponseString
            );

            if (linkPreviewResponse == null)
            {
                throw new LinkPreviewException(
                    System.Net.HttpStatusCode.InternalServerError,
                    "Failed to deserialize the API response."
                );
            }

            return linkPreviewResponse;
        }
        catch (Exception ex) when (ex is not LinkPreviewException)
        {
            mainApiException = new LinkPreviewException(
                $"An unexpected error occurred: {ex.Message}"
            );
        }
        catch (LinkPreviewException ex)
        {
            mainApiException = ex;
        }

        // 3. Lazy polyfills (if main API failed)
        foreach (var polyfill in this.polyfills.Where(p => !p.IsEager && p.CanHandle(url)))
        {
            var lazyResult = await polyfill.TryGetLinkPreviewAsync(url, cancellationToken);

            if (lazyResult != null)
            {
                lazyResult.IsPolyfill = true;
                return lazyResult;
            }
        }

        // 4. If all else fails, throw the main API exception or a generic one
        throw mainApiException ?? new LinkPreviewException("No preview available.");
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

        if (fields.Value.HasFlag(LinkPreviewOptionalField.Canonical))
            AppendField(sb, "canonical");
        if (fields.Value.HasFlag(LinkPreviewOptionalField.Locale))
            AppendField(sb, "locale");
        if (fields.Value.HasFlag(LinkPreviewOptionalField.SiteName))
            AppendField(sb, "site_name");
        if (fields.Value.HasFlag(LinkPreviewOptionalField.ImageX))
            AppendField(sb, "image_x");
        if (fields.Value.HasFlag(LinkPreviewOptionalField.ImageY))
            AppendField(sb, "image_y");
        if (fields.Value.HasFlag(LinkPreviewOptionalField.ImageSize))
            AppendField(sb, "image_size");
        if (fields.Value.HasFlag(LinkPreviewOptionalField.ImageType))
            AppendField(sb, "image_type");
        if (fields.Value.HasFlag(LinkPreviewOptionalField.Icon))
            AppendField(sb, "icon");
        if (fields.Value.HasFlag(LinkPreviewOptionalField.IconX))
            AppendField(sb, "icon_x");
        if (fields.Value.HasFlag(LinkPreviewOptionalField.IconY))
            AppendField(sb, "icon_y");
        if (fields.Value.HasFlag(LinkPreviewOptionalField.IconSize))
            AppendField(sb, "icon_size");
        if (fields.Value.HasFlag(LinkPreviewOptionalField.IconType))
            AppendField(sb, "icon_type");

        return sb.ToString().TrimEnd(',');
    }

    private static void AppendField(StringBuilder sb, string field)
    {
        sb.Append(field).Append(',');
    }
}
