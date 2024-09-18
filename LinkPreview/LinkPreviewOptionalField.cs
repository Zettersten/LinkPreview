namespace LinkPreview;

/// <summary>
/// Represents optional fields that can be requested from the LinkPreview API.
/// </summary>
[Flags]
public enum LinkPreviewOptionalField
{
    None = 0,
    Canonical = 1 << 0,
    Locale = 1 << 1,
    SiteName = 1 << 2,
    ImageX = 1 << 3,
    ImageY = 1 << 4,
    ImageSize = 1 << 5,
    ImageType = 1 << 6,
    Icon = 1 << 7,
    IconX = 1 << 8,
    IconY = 1 << 9,
    IconSize = 1 << 10,
    IconType = 1 << 11
}
