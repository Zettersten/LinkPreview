using LinkPreview.Polyfills;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LinkPreview
{
    /// <summary>
    /// Extension methods for setting up LinkPreview services.
    /// </summary>
    public static class LinkPreviewExtensions
    {
        /// <summary>
        /// Adds the LinkPreview service to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configuration">The configuration section for LinkPreview options.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddLinkPreviewService(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            services.AddLinkPreviewService(configuration.GetSection("LinkPreview"));
            return services;
        }

        /// <summary>
        /// Adds the LinkPreview service to the specified <see cref="IServiceCollection"/> with the provided configuration section.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configurationSection">The configuration section for LinkPreview options.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddLinkPreviewService(
            this IServiceCollection services,
            IConfigurationSection configurationSection
        )
        {
            services.Configure<LinkPreviewOptions>(configurationSection);
            return services.AddLinkPreviewService();
        }

        /// <summary>
        /// Adds the LinkPreview service to the specified <see cref="IServiceCollection"/> with the provided options.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="options">The action to configure the LinkPreview options.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddLinkPreviewService(
            this IServiceCollection services,
            Action<LinkPreviewOptions> options
        )
        {
            services.Configure(options);
            return services.AddLinkPreviewService();
        }

        /// <summary>
        /// Adds the LinkPreview service to the specified <see cref="IServiceCollection"/> with the provided options.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="optionsFactory">
        /// A factory function to create the LinkPreview options, given an <see cref="IServiceProvider"/>.
        /// </param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddLinkPreviewService(
            this IServiceCollection services,
            Func<IServiceProvider, LinkPreviewOptions> optionsFactory
        )
        {
            services.AddSingleton(provider =>
            {
                var options = optionsFactory(provider);
                return Options.Create(options);
            });

            return services.AddLinkPreviewService();
        }

        /// <summary>
        /// Adds the LinkPreview service to the specified <see cref="IServiceCollection"/> with default or preconfigured options.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddLinkPreviewService(this IServiceCollection services)
        {
            services.AddMemoryCache();

            services
                .AddHttpClient<ILinkPreviewService, LinkPreviewService>(
                    (serviceProvider, client) =>
                    {
                        var options = serviceProvider
                            .GetRequiredService<IOptions<LinkPreviewOptions>>()
                            .Value;
                        client.BaseAddress = new Uri(options.ApiBaseUrl);
                        client.DefaultRequestHeaders.Add("X-Linkpreview-Api-Key", options.ApiKey);
                    }
                )
                .AddStandardResilienceHandler();

            services.AddTransient<ILinkPreviewService, LinkPreviewService>();

            // Automatically register all ILinkPreviewPolyfill implementations
            var polyfillType = typeof(ILinkPreviewPolyfill);
            var polyfillImplementations = AppDomain
                .CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch
                    {
                        return [];
                    }
                })
                .Where(t =>
                    polyfillType.IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false }
                )
                .ToList();

            foreach (var impl in polyfillImplementations)
            {
                services.AddSingleton(typeof(ILinkPreviewPolyfill), impl);
            }
            // Add more polyfills here as needed

            return services;
        }
    }
}
