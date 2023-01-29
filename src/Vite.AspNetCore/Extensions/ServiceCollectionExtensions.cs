// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Vite.AspNetCore.Abstractions;
using Vite.AspNetCore.Services;

namespace Vite.AspNetCore.Extensions
{
	public static class ServiceCollectionExtensions
	{
		/// <summary>
		/// Adds the Vite Middleware service to the service collection.
		/// </summary>
		/// <param name="services">The service collection.</param>
		public static IServiceCollection AddViteDevMiddleware(this IServiceCollection services)
			=> services.AddSingleton<ViteDevMiddleware>();

		/// <summary>
		/// Adds the Vite Manifest Service to the service collection.
		/// </summary>
		/// <param name="services">The service collection.</param>
		/// <param name="optionsLifetime">The lifetime with which to register the Vite Manifest service in the container.</param>
		public static IServiceCollection AddViteManifest(this IServiceCollection services, ServiceLifetime optionsLifetime = ServiceLifetime.Scoped)
			=> services.AddViteManifest<ViteManifest>(optionsLifetime);

		/// <summary>
		/// Adds a custom Vite Manifest Service to the service collection.
		/// </summary>
		/// <param name="services">The service collection.</param>
		/// <param name="optionsLifetime">The lifetime with which to register the Vite Manifest service in the container.</param>
		public static IServiceCollection AddViteManifest<TViteManifest>(this IServiceCollection services, ServiceLifetime optionsLifetime)
			where TViteManifest : class, IViteManifest
		{
			return optionsLifetime switch
			{
				ServiceLifetime.Singleton => services.AddSingleton<IViteManifest, TViteManifest>(),
				ServiceLifetime.Scoped => services.AddScoped<IViteManifest, TViteManifest>(),
				ServiceLifetime.Transient => services.AddTransient<IViteManifest, TViteManifest>(),
				_ => services.AddScoped<IViteManifest, TViteManifest>(),
			};
		}
	}
}
