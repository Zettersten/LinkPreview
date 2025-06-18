namespace LinkPreview.Polyfills;

public interface ILinkPreviewPolyfill
{
    /// <summary>
    /// Returns true if this polyfill can handle the given URL.
    /// </summary>
    bool CanHandle(string url);

    /// <summary>
    /// Whether this polyfill should run eagerly (before main API) or lazily (after main API fails).
    /// </summary>
    bool IsEager { get; }

    /// <summary>
    /// Attempts to fetch a link preview for the given URL.
    /// Returns null if not handled or not applicable.
    /// </summary>
    Task<LinkPreviewResponse?> TryGetLinkPreviewAsync(
        string url,
        CancellationToken cancellationToken
    );
}
