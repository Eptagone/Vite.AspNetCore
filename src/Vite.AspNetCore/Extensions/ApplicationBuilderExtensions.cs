// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Vite.AspNetCore.Services;

namespace Vite.AspNetCore.Extensions;

/// <summary>
/// Vite extension methods for <see cref="IApplicationBuilder"/>.
/// </summary>
public static class ApplicationBuilderExtensions
{
	/// <summary>
	/// Registers the <b>Vite Dev Server</b> as the Static File Middleware.
	/// <para>
	/// Note that <b>this method will not work</b> if the Vite Dev Server is not running.
	/// </para>
	/// </summary>
	/// <param name="app">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
	/// <returns>The <see cref="IApplicationBuilder"/> instance this method extends.</returns>
	/// <exception cref="ArgumentNullException"></exception>
	[Obsolete("Use UseViteDevelopmentServer instead.")]
	public static IApplicationBuilder UseViteDevMiddleware(this IApplicationBuilder app)
	{
		return app.UseViteDevelopmentServerProxy(true);
	}

	/// <summary>
	/// Enables the Vite Development proxy middleware.
	/// By calling this method, tag helpers will render urls to the Vite Development Server.
	/// If the middleware is enabled, all requests will be proxied to the Vite Development Server.
	/// </summary>
	/// <param name="app">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
	/// <param name="useMiddleware">If true, a middleware will be registered to proxy all requests to the Vite Development Server.</param>
	/// <returns>The <see cref="IApplicationBuilder"/> instance this method extends.</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IApplicationBuilder UseViteDevelopmentServerProxy(this IApplicationBuilder app, bool useMiddleware = false)
	{
		if (app is null)
		{
			throw new ArgumentNullException(nameof(app));
		}

		if (useMiddleware)
		{
			// Enable the Vite Development Server middleware.
			ViteDevServerStatus.IsMiddlewareEnable = useMiddleware;
			// Register the middleware.
			app.UseMiddleware<ViteDevMiddleware>();
		}

		// Return the IApplicationBuilder instance.
		return app;
	}
}
