// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Vite.AspNetCore;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Vite services to the service collection. This includes the Vite Manifest Service and the Vite Dev Middleware Service.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">An <see cref="Action{ViteOptions}"/> to configure the provided <see cref="ViteOptions"/>.</param>
    /// <param name="optionsLifetime">The lifetime with which to register the Vite Manifest service in the container.</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddViteServices(
        this IServiceCollection services,
        Action<ViteOptions>? configure = null,
        ServiceLifetime optionsLifetime = ServiceLifetime.Singleton
    )
    {
        return services.AddViteServices<ViteManifest>(configure, optionsLifetime);
    }

    /// <summary>
    /// Adds all Vite services to the service collection. This includes the Vite Manifest Service and the Vite Dev Middleware Service.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">An <see cref="Action{ViteOptions}"/> to configure the provided <see cref="ViteOptions"/>.</param>
    /// <param name="optionsLifetime">The lifetime with which to register the Vite Manifest service in the container.</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddViteServices<T>(
        this IServiceCollection services,
        Action<ViteOptions>? configure = null,
        ServiceLifetime optionsLifetime = ServiceLifetime.Singleton
    )
        where T : class, IViteManifest
    {
        if (configure is null)
        {
            services.SetOptions();
        }
        else
        {
            services.Configure(configure);
        }

        return services.ConfigureServices<T>(optionsLifetime);
    }

    /// <summary>
    /// Adds all Vite services to the service collection. This includes the Vite Manifest Service and the Vite Dev Middleware Service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The Vite configuration options. If null, the default options will be used.</param>
    /// <param name="optionsLifetime">The lifetime with which to register the Vite Manifest service in the container.</param>
    public static IServiceCollection AddViteServices(
        this IServiceCollection services,
        ViteOptions options,
        ServiceLifetime optionsLifetime = ServiceLifetime.Singleton
    )
    {
        return services.AddViteServices<ViteManifest>(options, optionsLifetime);
    }

    /// <summary>
    /// Adds all Vite services to the service collection. This includes the Vite Manifest Service and the Vite Dev Middleware Service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The Vite configuration options. If null, the default options will be used.</param>
    /// <param name="optionsLifetime">The lifetime with which to register the Vite Manifest service in the container.</param>
    public static IServiceCollection AddViteServices<T>(
        this IServiceCollection services,
        ViteOptions options,
        ServiceLifetime optionsLifetime
    )
        where T : class, IViteManifest
    {
        return services.SetOptions(options).ConfigureServices<T>(optionsLifetime);
    }

    /// <summary>
    /// Adds all Vite services to the service collection. This includes the Vite Manifest Service and the Vite Dev Middleware Service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="optionsLifetime">The lifetime with which to register the Vite Manifest service in the container.</param>
    public static IServiceCollection AddViteServices(
        this IServiceCollection services,
        ServiceLifetime optionsLifetime
    )
    {
        return services.AddViteServices<ViteManifest>(optionsLifetime);
    }

    /// <summary>
    /// Adds all Vite services to the service collection. This includes the Vite Manifest Service and the Vite Dev Middleware Service.
    /// </summary>
    /// <typeparam name="T">The type of the Vite Manifest Service.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="optionsLifetime">The lifetime with which to register the Vite Manifest service in the container.</param>
    public static IServiceCollection AddViteServices<T>(
        this IServiceCollection services,
        ServiceLifetime optionsLifetime
    )
        where T : class, IViteManifest
    {
        return services.SetOptions().ConfigureServices<T>(optionsLifetime);
    }

    private static IServiceCollection SetOptions(
        this IServiceCollection services,
        ViteOptions? options = null
    )
    {
        // Configure the Vite options
        if (options is null)
        {
            // Add the Vite options from the configuration
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            IConfiguration configuration = serviceProvider.GetRequiredService<IConfiguration>();
            services.Configure<ViteOptions>(configuration.GetSection(ViteOptions.Vite));
        }
        else
        {
            // Add the Vite options from the options parameter
            services.AddSingleton(Options.Create(options));
        }

        return services;
    }

    private static IServiceCollection ConfigureServices<T>(
        this IServiceCollection services,
        ServiceLifetime lifetime
    )
        where T : class, IViteManifest
    {
        // Add http client factory if not already added
        if (services.All(x => x.ServiceType != typeof(IHttpClientFactory)))
        {
            services.AddHttpClient();
        }

        // Add an HttpClient for the Vite Dev Server
        services
            .AddHttpClient(ViteDevServerMiddleware.HTTP_CLIENT_NAME)
            .ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            })
            .ConfigureHttpClient(
                (_, client) =>
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("*/*", 0.1)
                    )
            )
            .RemoveAllLoggers();

        // Add the Vite Dev Server Launcher
        services.TryAddSingleton<ViteDevServerLauncher>();

        // Add the Vite Dev Server Service
        services.TryAddSingleton<IViteDevServerStatus, ViteDevServerStatus>();

        // Add the manifest service
        ServiceDescriptor descriptor = new(typeof(IViteManifest), typeof(T), lifetime);
        services.Add(descriptor);

        // Add the middleware for the Vite development server
        services.TryAddSingleton<ViteDevServerMiddleware>();

        // Add the ViteTagHelperMonitor
        services.TryAddScoped<ViteTagHelperMonitor>();

        return services;
    }
}
