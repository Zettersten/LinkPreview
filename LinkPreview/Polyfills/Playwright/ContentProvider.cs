using LinkPreview.Polyfills.Squidlr;
using Microsoft.Playwright;
using Polly;

namespace LinkPreview.Polyfills.Playwright;

public sealed class ContentProvider(UrlResolver urlResolver) : IAsyncDisposable
{
    private static volatile bool disposed = false;
    private static readonly SemaphoreSlim browserInitSemaphore = new(1, 1);
    private static readonly SemaphoreSlim installSemaphore = new(1, 1);

    private static IPlaywright? playwrightInstance;
    private static IBrowser? sharedBrowser;

    public async Task<Content> GetContent(string url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL must not be null or empty.", nameof(url));

        var contentIdentifier = urlResolver.ResolveUrl(url);

        if (
            contentIdentifier.Platform == SocialMediaPlatform.Unknown
            || contentIdentifier.Platform == SocialMediaPlatform.Tiktok
            || contentIdentifier.Platform == SocialMediaPlatform.Facebook
            || contentIdentifier.Platform == SocialMediaPlatform.LinkedIn
        )
        {
            throw new ArgumentException(
                $"Unsupported platform. ({contentIdentifier.Platform})",
                nameof(url)
            );
        }

        var browser = await GetSharedBrowserAsync(cancellationToken);

        var contextOptions = GetStealthContextOptions();

        await using var context = await browser.NewContextAsync(contextOptions);

        var page = await context.NewPageAsync();

        await page.RouteAsync(
            "**/*",
            async route =>
            {
                var req = route.Request;
                if (req.ResourceType is "stylesheet" or "font")
                    await route.AbortAsync();
                else
                    await route.ContinueAsync();
            }
        );

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(60)); // Overall timeout

        // Retry policy for transient failures
        var retryPolicy = Policy
            .Handle<PlaywrightException>()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(500 * attempt));

        await retryPolicy.ExecuteAsync(async () =>
        {
            cts.Token.ThrowIfCancellationRequested();

            var response = await page.GotoAsync(
                contentIdentifier.Url,
                new PageGotoOptions
                {
                    Timeout = 30000, // longer timeout for complex JS
                    WaitUntil = WaitUntilState.DOMContentLoaded // wait for "no more network"
                }
            );

            // Handle JS-based client-side redirects (e.g. location.replace or single-page apps)
            for (int i = 0; i < 3; i++)
            {
                await page.WaitForTimeoutAsync(1000); // let scripts run

                var currentUrl = page.Url;

                var hasRedirected = !string.Equals(
                    currentUrl,
                    contentIdentifier.Url,
                    StringComparison.OrdinalIgnoreCase
                );

                if (!hasRedirected)
                    break;

                contentIdentifier = urlResolver.ResolveUrl(currentUrl);

                await page.GotoAsync(
                    currentUrl,
                    new PageGotoOptions
                    {
                        Timeout = 15000,
                        WaitUntil = WaitUntilState.DOMContentLoaded
                    }
                );
            }

            // Wait for known stable DOM marker (if possible)
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded); // just to double-confirm

            // Optional: wait for a specific selector (like a footer or main content)
            // await page.WaitForSelectorAsync("body", new() { Timeout = 5000 });
        });

        cancellationToken.ThrowIfCancellationRequested();

        var html = await page.ContentAsync();

        return new Content(html, contentIdentifier);
    }

    private static bool installChecked = false;

    private static async Task EnsureBrowsersInstalledAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (installChecked)
            return;

        if (!await installSemaphore.WaitAsync(TimeSpan.FromSeconds(60), cancellationToken))
            throw new TimeoutException("Install initialization timeout");

        try
        {
            if (installChecked)
                return;

            var exitCode = Microsoft.Playwright.Program.Main(["install"]);

            if (exitCode != 0)
                throw new Exception($"Playwright exited with code {exitCode}");

            installChecked = true;
        }
        finally
        {
            installSemaphore.Release();
        }
    }

    private static async Task<IBrowser> GetSharedBrowserAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (sharedBrowser is not null)
            return sharedBrowser;

        await EnsureBrowsersInstalledAsync(cancellationToken);

        if (!await browserInitSemaphore.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken))
            throw new TimeoutException("Browser initialization timeout");

        try
        {
            if (sharedBrowser is not null)
                return sharedBrowser;

            playwrightInstance ??= await Microsoft.Playwright.Playwright.CreateAsync();

            sharedBrowser = await playwrightInstance.Chromium.LaunchAsync(
                new BrowserTypeLaunchOptions
                {
                    Headless = true,
                    Args = ["--disable-blink-features=AutomationControlled"]
                }
            );

            return sharedBrowser;
        }
        finally
        {
            browserInitSemaphore.Release();
        }
    }

    private static BrowserNewContextOptions GetStealthContextOptions()
    {
        var ua = GetRandomUserAgent();
        var (width, height) = GetRandomViewport();
        var timezone = GetRandomTimezone();
        var locale = GetRandomLocale();

        return new BrowserNewContextOptions
        {
            // 🔐 Basic stealth settings
            UserAgent = ua,
            ViewportSize = new ViewportSize { Width = width, Height = height },
            TimezoneId = timezone,
            Locale = locale,
            JavaScriptEnabled = true,
            IsMobile = false,
            HasTouch = false,

            // 🎨 Media features for realism
            ColorScheme = ColorScheme.Light,
            Contrast = Contrast.NoPreference,
            ForcedColors = ForcedColors.None,
            ReducedMotion = ReducedMotion.NoPreference,

            // 📦 Typical desktop config
            DeviceScaleFactor = 1.0f,
            AcceptDownloads = true,
            BypassCSP = false,
            IgnoreHTTPSErrors = false,
            StrictSelectors = true,

            // 📜 Security & permissions
            HttpCredentials = null,
            Permissions = [],
            ServiceWorkers = ServiceWorkerPolicy.Allow,

            // 🌐 Network
            Offline = false,
            Proxy = null,

            // 🎥 HAR/Video recording disabled
            RecordHarContent = null,
            RecordHarMode = null,
            RecordHarOmitContent = false,
            RecordHarPath = null,
            RecordHarUrlFilter = null,
            RecordHarUrlFilterRegex = null,
            RecordHarUrlFilterString = null,
            RecordVideoDir = null,
            RecordVideoSize = null,

            // 🖥️ Screen emulation
            ScreenSize = new ScreenSize { Width = width, Height = height },

            // 💾 Session state
            StorageState = null,
            StorageStatePath = null
        };
    }

    private static readonly string[] userAgents =
    [
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 13_4) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 13_3_1) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.4 Safari/605.1.15"
    ];

    private static string GetRandomUserAgent()
    {
        return userAgents[Random.Shared.Next(userAgents.Length)];
    }

    private static (int width, int height) GetRandomViewport()
    {
        var viewports = new[] { (1280, 720), (1366, 768), (1440, 900), (1920, 1080), (1600, 900) };
        return viewports[Random.Shared.Next(viewports.Length)];
    }

    private static string GetRandomTimezone()
    {
        var timezones = new[]
        {
            "America/New_York",
            "Europe/London",
            "America/Los_Angeles",
            "Australia/Sydney"
        };

        return timezones[Random.Shared.Next(timezones.Length)];
    }

    private static string GetRandomLocale()
    {
        var locales = new[] { "en-US", "en-GB" };
        return locales[Random.Shared.Next(locales.Length)];
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        try
        {
            playwrightInstance?.Dispose();

            if (sharedBrowser is not null)
            {
                await sharedBrowser.DisposeAsync();
            }
        }
        finally
        {
            disposed = true;
        }
    }
}
