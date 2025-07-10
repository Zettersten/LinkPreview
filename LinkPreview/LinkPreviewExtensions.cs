using LinkPreview.Polyfills;
using LinkPreview.Polyfills.Squidlr;
using LinkPreview.Polyfills.Squidlr.Abstractions;
using LinkPreview.Polyfills.Squidlr.Facebook;
using LinkPreview.Polyfills.Squidlr.Instagram;
using LinkPreview.Polyfills.Squidlr.LinkedIn;
using LinkPreview.Polyfills.Squidlr.Tiktok;
using LinkPreview.Polyfills.Squidlr.Twitter;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

            services.AddHttpClient<LinkPreviewUrlVerifier>().AddStandardResilienceHandler();
            services.AddHttpClient<LinkPreviewFallbackService>().AddStandardResilienceHandler();

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
                .AddStandardResilienceHandler(x =>
                    x.TotalRequestTimeout =
                        new Microsoft.Extensions.Http.Resilience.HttpTimeoutStrategyOptions
                        {
                            Timeout = TimeSpan.FromMinutes(2)
                        }
                );

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

            foreach (var implementation in polyfillImplementations)
            {
                services.AddSingleton(typeof(ILinkPreviewPolyfill), implementation);
            }

            services
                .AddOptions<SquidlrOptions>()
                .Configure(options =>
                {
                    options.FacebookHostUri = new Uri("https://www.facebook.com");
                    options.InstagramHostUri = new Uri("https://www.instagram.com");
                    options.LinkedInHostUri = new Uri("https://www.linkedin.com");
                    options.TiktokHostUri = new Uri("https://www.tiktok.com");
                    options.TwitterAuthorizationBearerToken =
                        "AAAAAAAAAAAAAAAAAAAAANRILgAAAAAAnNwIzUejRCOuH5E6I8xnZz4puTs%3D1Zv7ttfk8LF81IUq16cHjhLTvJu4FA33AGWWjCpTnA";
                    options.TwitterApiHostUri = new Uri("https://api.twitter.com");
                });

            services.AddSingleton<Polyfills.Playwright.ContentProvider>();

            services.AddMemoryCache();

            services.AddSingleton(sp => new UrlResolver(
                sp.GetServices<IUrlResolver>().ToList().AsReadOnly()
            ));
            services.AddSingleton(sp => new Polyfills.Squidlr.ContentProvider(
                sp.GetServices<IContentProvider>().ToList().AsReadOnly(),
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<ILogger<Polyfills.Squidlr.ContentProvider>>()
            ));

            // add supported social media platforms
            services.AddFacebook();
            services.AddInstagram();
            services.AddLinkedIn();
            services.AddTiktok();
            services.AddTwitter();

            return services;
        }
    }
}
