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
		public static IServiceCollection AddViteManifest(this IServiceCollection services)
			=> services.AddScoped<IViteManifest, ViteManifest>();
	}
}
