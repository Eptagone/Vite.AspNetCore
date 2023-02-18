// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Vite.AspNetCore.Services;

namespace Vite.AspNetCore.Extensions
{
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
		public static IApplicationBuilder UseViteDevMiddleware(this IApplicationBuilder app)
			=> app.UseMiddleware<ViteDevMiddleware>();
	}
}
