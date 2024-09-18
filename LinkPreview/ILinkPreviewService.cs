using System.Diagnostics.CodeAnalysis;

namespace LinkPreview;

/// <summary>
/// Provides services for interacting with the LinkPreview API.
/// </summary>
public interface ILinkPreviewService
{
    /// <summary>
    /// Retrieves a link preview for the specified URL.
    /// </summary>
    /// <param name="url"></param>
    /// <param name="optionalFields"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>
    /// Link preview information for the specified URL.
    /// </returns>
    [RequiresUnreferencedCode("")]
    [RequiresDynamicCode("")]
    Task<LinkPreviewResponse> GetLinkPreviewAsync(
        string url,
        LinkPreviewOptionalField? optionalFields = null,
        CancellationToken cancellationToken = default
    );
}
