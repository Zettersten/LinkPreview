using System.Text.RegularExpressions;

namespace LinkPreview.Polyfills.Squidlr.Twitter.Utilities;

public static partial class GuestTokenUtilities
{
    [GeneratedRegex(@"gt=(?<guestToken>[0-9]*)", RegexOptions.IgnoreCase)]
    private static partial Regex GuestTokenRegex();

    public static string? ExtractGuestToken(string content)
    {
        ArgumentException.ThrowIfNullOrEmpty(content);

        var match = GuestTokenRegex().Match(content);
        if (!match.Success)
            return null;

        return match.Groups["guestToken"].Value;
    }
}
