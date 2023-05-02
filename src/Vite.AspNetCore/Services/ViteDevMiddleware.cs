// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Vite.AspNetCore.Utilities;

namespace Vite.AspNetCore.Services;

/// <summary>
/// Represents a middleware that proxies requests to the Vite Dev Server.
/// </summary>
public class ViteDevMiddleware : IMiddleware, IDisposable
{
	private readonly ILogger<ViteDevMiddleware> _logger;
	private readonly string _viteServerBaseUrl;
	private readonly NodeScriptRunner? _scriptRunner;
	private readonly ViteOptions _viteOptions;

	private static readonly Regex ViteReadyRegex = new(@"^\s*VITE\sv\d+\.\d+\.\d+\s+ready\sin\s\d+(?:\.\d+)?\s\w+$", RegexOptions.Compiled);

	/// <summary>
	/// Optional. This flag is used to know if the middleware is waiting for the Vite development server to start.
	/// </summary>
	private bool waitForDevServer;

	/// <summary>
	/// Initializes a new instance of the <see cref="ViteDevMiddleware"/> class.
	/// </summary>
	/// <param name="logger">The <see cref="ILogger{ViteDevMiddleware}"/> instance.</param>
	/// <param name="configuration">The <see cref="IConfiguration"/> instance.</param>
	/// <param name="environment">The <see cref="IWebHostEnvironment"/> instance.</param>
	/// <param name="lifetime">The <see cref="IHostApplicationLifetime"/> instance.</param>
	public ViteDevMiddleware(ILogger<ViteDevMiddleware> logger, IConfiguration configuration, IWebHostEnvironment environment, IHostApplicationLifetime lifetime)
	{
		ViteStatusService.IsDevServerRunning = true;
		// Set the logger.
		this._logger = logger;
		// Read the Vite options from the configuration.
		this._viteOptions = configuration.GetSection(ViteOptions.Vite).Get<ViteOptions>();
		// Get the port from the configuration.
		var port = this._viteOptions.Server.Port;
		// Check if https is enabled.
		var https = this._viteOptions.Server.Https;
		// Build the base url.
		this._viteServerBaseUrl = $"{(https ? "https" : "http")}://localhost:{port}";

		// Prepare and run the Vite Dev Server if AutoRun is true and the middleware is enabled.
		if (this._viteOptions.Server.AutoRun && ViteStatusService.IsMiddlewareRegistered)
		{
			// Set the waitForDevServer flag to true.
			this.waitForDevServer = true;
			// Gets the package manager command.
			var pkgManagerCommand = this._viteOptions.PackageManager;
			// Gets the working directory.
			var workingDirectory = this._viteOptions.WorkingDirectory ?? environment.ContentRootPath;
			// Gets the script name.= to run the Vite Dev Server.
			var scriptName = this._viteOptions.Server.ScriptName;
			// Create a new instance of the NodeScriptRunner class.
			this._scriptRunner = new NodeScriptRunner(logger, pkgManagerCommand, scriptName, workingDirectory, lifetime.ApplicationStopping);


			// Read output reader.
			this._scriptRunner.StdOutReader.OnReceivedLine += line =>
			{
				// Check if the line contains the base url.
				if (ViteReadyRegex.IsMatch(line))
				{
					logger.LogInformation("The vite development server was started. Starting to proxy requests to '{ViteServerBaseUrl}'.", this._viteServerBaseUrl);
					// Set the waitForDevServer flag to false
					this.waitForDevServer = false;
				}
			};

			logger.LogInformation(
				"The middleware has called the dev script. It may take a few seconds before the Vite Dev Server becomes available.");
		}
	}

	/// <inheritdoc />
	public async Task InvokeAsync(HttpContext context, RequestDelegate next)
	{
		// If the middleware is not registered, call the next middleware.
		if (!ViteStatusService.IsMiddlewareRegistered)
		{
			this._logger.LogWarning("Ups, did you forgot to register the middleware? Use app.UseViteDevMiddleware() in your Startup.cs or Program.cs file.");
			await next(context);
			return;
		}

		// If the request doesn't have an endpoint, the request path is not null and the request method is GET, proxy the request to the Vite Development Server.
		if (context.GetEndpoint() == null && context.Request.Path.HasValue && context.Request.Method == HttpMethod.Get.Method)
		{
			// Get the request path
			var path = context.Request.Path.Value;
			// Proxy the request to the Vite Dev Server.
			await this.ProxyAsync(context, next, path);
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
		using HttpClient client = new() { BaseAddress = new Uri(this._viteServerBaseUrl) };

		// If the waitForDevServer flag is true, wait for the Vite development server to start.
		if (this.waitForDevServer)
		{
			// Wait for the server to start until the timeout is reached or the server is found.
			var timeout = TimeSpan.FromSeconds(this._viteOptions.Server.TimeOut);
			// Smaller increments mean faster discover but potentially more loops
			var increment = TimeSpan.FromMilliseconds(200);
			var waiting = new TimeSpan(0);

			// Wait for the server to start until the timeout is reached or the server is found.
			while (this.waitForDevServer && waiting < timeout)
			{
				waiting = waiting.Add(increment);
				await Task.Delay(increment);
			}

			// If the waitForDevServer flag is true, log the timeout.
			if (this.waitForDevServer)
			{
				this._logger.LogWarning("The Vite development server did not start within {TotalSeconds} seconds.", timeout.TotalSeconds);
				// Set the waitForDevServer flag to false.
				this.waitForDevServer = false;
			}
		}

		try
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
		catch (HttpRequestException exp)
		{
			// Log the exception.
			this._logger.LogWarning("{Message}. Make sure the Vite development server is running.", exp.Message);
			await next(context);
		}
	}

	void IDisposable.Dispose()
	{
		// If the script runner was defined, dispose it.
		if (this._scriptRunner != null)
		{
			((IDisposable)this._scriptRunner).Dispose();
		}

		GC.SuppressFinalize(this);
	}
}
