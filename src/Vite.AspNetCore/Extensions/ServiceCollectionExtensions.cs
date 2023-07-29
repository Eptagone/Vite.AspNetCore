// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Vite.AspNetCore.Abstractions;
using Vite.AspNetCore.Services;

namespace Vite.AspNetCore.Extensions;

public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Adds all Vite services to the service collection. This includes the Vite Manifest Service and the Vite Dev Middleware Service.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="options">The Vite configuration options. If null, the default options will be used.</param>
	/// <param name="optionsLifetime">The lifetime with which to register the Vite Manifest service in the container.</param>
	public static IServiceCollection AddViteServices(
		this IServiceCollection services,
		ViteOptions? options = null,
		ServiceLifetime optionsLifetime = ServiceLifetime.Singleton)
	{
		return services.AddViteServices<ViteManifest>(options, optionsLifetime);
	}

	/// <summary>
	/// Adds all Vite services to the service collection. This includes the Vite Manifest Service and the Vite Dev Middleware Service.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="options">The Vite configuration options. If null, the default options will be used.</param>
	/// <param name="optionsLifetime">The lifetime with which to register the Vite Manifest service in the container.</param>
	public static IServiceCollection AddViteServices<T>(this IServiceCollection services, ViteOptions? options = null, ServiceLifetime optionsLifetime = ServiceLifetime.Singleton)
		where T : class, IViteManifest
	{
		// Add http client factory if not already added 
		if (services.All(x => x.ServiceType != typeof(IHttpClientFactory)))
		{
			services.AddHttpClient();
		}

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


		// Add the status service
		services.TryAddScoped<ViteStatusService>();

		// Add the manifest service
		ServiceDescriptor descriptor = new(typeof(IViteManifest), typeof(T), optionsLifetime);
		services.Add(descriptor);

		// Add the middleware for the Vite development server
		services.TryAddSingleton<ViteDevMiddleware>();

		return services;
	}

	/// <summary>
	/// Adds all Vite services to the service collection. This includes the Vite Manifest Service and the Vite Dev Middleware Service.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="optionsLifetime">The lifetime with which to register the Vite Manifest service in the container.</param>
	public static IServiceCollection AddViteServices(this IServiceCollection services, ServiceLifetime optionsLifetime)
	{
		return services.AddViteServices<ViteManifest>(null, optionsLifetime);
	}

	/// <summary>
	/// Adds all Vite services to the service collection. This includes the Vite Manifest Service and the Vite Dev Middleware Service.
	/// </summary>
	/// <typeparam name="T">The type of the Vite Manifest Service.</typeparam>
	/// <param name="services">The service collection.</param>
	/// <param name="optionsLifetime">The lifetime with which to register the Vite Manifest service in the container.</param>
	public static IServiceCollection AddViteServices<T>(
		this IServiceCollection services,
		ServiceLifetime optionsLifetime)
		where T : class, IViteManifest
	{
		return services.AddViteServices<T>(null, optionsLifetime);
	}
}
