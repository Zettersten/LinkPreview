using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace LinkPreview.Tests;

public sealed class LinkPreviewServiceFixture
{
    public ServiceProvider ServiceProvider { get; }

    public LinkPreviewServiceFixture()
    {
        var services = new ServiceCollection();

        var testProjectDirectory = Directory
            .GetParent(Directory.GetCurrentDirectory())
            ?.Parent?.Parent?.FullName;

        ArgumentException.ThrowIfNullOrEmpty(testProjectDirectory);

        var envFilePath = Path.Combine(testProjectDirectory, ".env");

        if (!File.Exists(envFilePath))
        {
            throw new ArgumentException(".env not found");
        }

        var apiKeyFromEnvFile = File.ReadAllText(envFilePath).Trim();

        ArgumentException.ThrowIfNullOrEmpty(apiKeyFromEnvFile);

        // Register dependencies
        services.AddMemoryCache();
        services.AddLinkPreviewService(
            (sp) =>
            {
                var memCache = sp.GetRequiredService<IMemoryCache>();

                return new LinkPreviewOptions { ApiKey = apiKeyFromEnvFile };
            }
        );

        this.ServiceProvider = services.BuildServiceProvider();
    }
}
