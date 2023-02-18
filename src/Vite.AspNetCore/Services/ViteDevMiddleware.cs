// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Vite.AspNetCore.Services
{
	/// <summary>
	/// Represents a middleware that proxies requests to the Vite Dev Server.
	/// </summary>
	public class ViteDevMiddleware : IMiddleware, IDisposable
	{
		private readonly NodeScriptRunner _scriptRunner;
		private readonly string _viteServerBaseUrl;

		/// <summary>
		/// Initializes a new instance of the <see cref="ViteDevMiddleware"/> class.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger{ViteDevMiddleware}"/> instance.</param>
		/// <param name="configuration">The <see cref="IConfiguration"/> instance.</param>
		/// <param name="environment">The <see cref="IWebHostEnvironment"/> instance.</param>
		public ViteDevMiddleware(ILogger<ViteDevMiddleware> logger, IConfiguration configuration, IWebHostEnvironment environment)
		{
			// Get the port from the configuration.
			var port = configuration.GetValue("Vite:Server:Port", 5173);
			// Check if https is enabled.
			var https = configuration.GetValue("Vite:Server:Https", false);
			// Build the base url.
			this._viteServerBaseUrl = $"{(https ? "https" : "http")}://localhost:{port}";

			// Gets the package manager command.
			var pkgManagerCommand = configuration.GetValue("Vite:PackageManager", "npm");
			// Gets the working directory.
			var workingDirectory = configuration.GetValue("Vite:WorkingDirectory", environment.ContentRootPath);
			// Gets the script name.= to run the Vite Dev Server.
			var scriptName = configuration.GetValue("Vite:Server:ScriptName", "dev");
			// Create a new instance of the NodeScriptRunner class.
			this._scriptRunner = new NodeScriptRunner(pkgManagerCommand, scriptName, workingDirectory);
			// Attach the logger to the script runner.
			this._scriptRunner.AttachToLogger(logger);
		}

		/// <inheritdoc />
		public async Task InvokeAsync(HttpContext context, RequestDelegate next)
		{
			// If the request path is not null, process.
			if (context.Request.Path.HasValue)
			{
				// Get the request path
				var path = context.Request.Path.Value;
				// Proxy the request to the Vite Dev Server.
				await ProxyAsync(context, next, path);
			}
			// If the request path is null, call the next middleware.
			else
			{
				await next(context);
			}
		}

		/// <summary>
		/// Proxies the request to the Vite Dev Server.
		/// </summary>
		/// <param name="context">The <see cref="HttpContext"/> instance.</param>
		/// <param name="path">The request path.</param>
		/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
		private async Task ProxyAsync(HttpContext context, RequestDelegate next, string path)
		{
			using (HttpClient client = new() { BaseAddress = new Uri(this._viteServerBaseUrl) })
			{
				// Get the requested path from the Vite Dev Server.
				var response = await client.GetAsync(path);
				// If the response is successful, process.
				if (response.IsSuccessStatusCode)
				{
					// Get the response content.
					var content = await response.Content.ReadAsByteArrayAsync();
					// Get the response content type.
					var contentType = response.Content.Headers.ContentType?.MediaType;
					// Set the response content type.
					context.Response.ContentType = contentType ?? "application/octet-stream";
					// Set the response content length.
					context.Response.ContentLength = content.Length;
					// Write the response content.
					await context.Response.Body.WriteAsync(content);
				}
				// Otherwise, call the next middleware.
				else
				{
					await next(context);
				}
			}
		}

		void IDisposable.Dispose()
		{
			((IDisposable)this._scriptRunner).Dispose();
			GC.SuppressFinalize(this);
		}
	}
}
