// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vite.AspNetCore.Abstractions;
using Vite.AspNetCore.Services;

namespace Vite.AspNetCore.Extensions;

public static class ServiceCollectionExtensions
{
	// This line is temporary, it will be removed when obsolete methods are removed.
	private static bool IsStatusServiceAdded = false;

	/// <summary>
	/// Adds all Vite services to the service collection. This includes the Vite Manifest Service and the Vite Dev Middleware Service.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="optionsLifetime">The lifetime with which to register the Vite Manifest service in the container.</param>
	public static IServiceCollection AddViteServices(this IServiceCollection services, ServiceLifetime optionsLifetime = ServiceLifetime.Singleton)
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
		ServiceLifetime optionsLifetime = ServiceLifetime.Singleton)
		where T : class, IViteManifest
	{
		services.TryAddScoped<ViteStatusService>();
		ServiceDescriptor descriptor = new(typeof(IViteManifest), typeof(T), optionsLifetime);
		services.Add(descriptor);
		services.TryAddSingleton<ViteDevMiddleware>();
		return services;
	}

	/// <summary>
	/// Adds the Vite Middleware service to the service collection.
	/// </summary>
	/// <param name="services">The service collection.</param>
	[Obsolete("Use AddViteDevServices instead.")]
	public static IServiceCollection AddViteDevMiddleware(this IServiceCollection services)
	{
		if (!IsStatusServiceAdded)
		{
			services.TryAddScoped<ViteStatusService>();
			IsStatusServiceAdded = true;
		}

		services.TryAddSingleton<ViteDevMiddleware>();
		return services;
	}

	/// <summary>
	/// Adds the Vite Manifest Service to the service collection.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="optionsLifetime">The lifetime with which to register the Vite Manifest service in the container.</param>
	[Obsolete("Use AddViteDevServices instead.")]
	public static IServiceCollection AddViteManifest(this IServiceCollection services, ServiceLifetime optionsLifetime = ServiceLifetime.Scoped)
		=> services.AddViteManifest<ViteManifest>(optionsLifetime);

	/// <summary>
	/// Adds a custom Vite Manifest Service to the service collection.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="optionsLifetime">The lifetime with which to register the Vite Manifest service in the container.</param>
	[Obsolete("Use AddViteDevServices instead.")]
	public static IServiceCollection AddViteManifest<TViteManifest>(this IServiceCollection services, ServiceLifetime optionsLifetime)
		where TViteManifest : class, IViteManifest
	{
		if (!IsStatusServiceAdded)
		{
			services.TryAddScoped<ViteStatusService>();
			IsStatusServiceAdded = true;
		}

		ServiceDescriptor descriptor = new(typeof(IViteManifest), typeof(TViteManifest), optionsLifetime);
		services.Add(descriptor);

		return services;
	}
}
