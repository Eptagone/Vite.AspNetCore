// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Vite.AspNetCore.Services;

/// <summary>
/// Represents a middleware that proxies requests to the Vite Dev Server.
/// </summary>
internal class ViteDevMiddleware : IMiddleware
{
	private readonly ILogger<ViteDevMiddleware> _logger;
	private readonly IHttpClientFactory _clientFactory;
	private readonly string _viteServerBaseUrl;
	private readonly ViteOptions _viteOptions;

	/// <summary>
	/// Optional. This flag is used to know if the middleware is waiting for the Vite development server to start.
	/// </summary>
	private bool _waitForDevServer;

	/// <summary>
	/// Initializes a new instance of the <see cref="ViteDevMiddleware"/> class.
	/// </summary>
	/// <param name="logger">The logger service.</param>
	/// <param name="options">Options for the middleware.</param>
	/// <param name="clientFactory">The <see cref="IHttpClientFactory"/> instance.</param>
	/// <param name="launchManager">The launch manager to launch the Vite development server.</param>
	public ViteDevMiddleware(
		ILogger<ViteDevMiddleware> logger,
		IOptions<ViteOptions> options,
		IHttpClientFactory clientFactory,
		ViteServerLaunchManager launchManager)
	{
		ViteStatusService.IsDevServerRunning = true;
		// Set the logger.
		this._logger = logger;
		// Set the http client factory
		this._clientFactory = clientFactory;

		// Read the Vite options from the configuration.
		this._viteOptions = options.Value;
		// Get the port and host from the configuration.
		var host = options.Value.Server.Host;
		var port = options.Value.Server.Port;
		// Check if https is enabled.
		var https = options.Value.Server.Https;
		// Build the base url.
		this._viteServerBaseUrl = $"{(https ? "https" : "http")}://{host}:{port}";

		// Prepare and run the Vite Dev Server if AutoRun is true and the middleware is enabled.
		if (options.Value.Server.AutoRun && ViteStatusService.IsMiddlewareRegistered)
		{
			// Set the waitForDevServer flag to true.
			this._waitForDevServer = true;

			// Launch the Vite Development Server.
			launchManager.LaunchDevelopmentServer();
		}
	}

	/// <inheritdoc />
	public async Task InvokeAsync(HttpContext context, RequestDelegate next)
	{
		// If the request doesn't have an endpoint, the request path is not null and the request method is GET, proxy the request to the Vite Development Server.
		if (context.GetEndpoint() == null && context.Request.Path.HasValue &&
			context.Request.Method == HttpMethod.Get.Method)
		{
			// Get the request path
			var path = context.Request.GetEncodedPathAndQuery();
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
	/// <param name="next">The next handler in the request pipeline</param>
	/// <param name="path">The request path.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	private async Task ProxyAsync(HttpContext context, RequestDelegate next, string path)
	{
		// Initialize a "new" instance of the HttpClient class via the HttpClientFactory.
		using var client = this._clientFactory.CreateClient();
		client.BaseAddress = new Uri(this._viteServerBaseUrl);

		// Pass "Accept" header from the original request.
		if (context.Request.Headers.ContainsKey("Accept"))
		{
			client.DefaultRequestHeaders.Add("Accept", context.Request.Headers.Accept.ToList());
		}

		// If the waitForDevServer flag is true, wait for the Vite development server to start.
		if (this._waitForDevServer)
		{
			// Wait for the server to start until the timeout is reached or the server is found.
			var timeout = TimeSpan.FromSeconds(this._viteOptions.Server.TimeOut);
			// Smaller increments mean faster discover but potentially more loops
			var increment = TimeSpan.FromMilliseconds(200);
			var waiting = new TimeSpan(0);

			// Wait for the server to start until the timeout is reached or the server is found.
			while (this._waitForDevServer && waiting < timeout)
			{
				waiting = waiting.Add(increment);
				await Task.Delay(increment);
			}

			// If the waitForDevServer flag is true, log the timeout.
			if (this._waitForDevServer)
			{
				this._logger.LogWarning("The Vite development server did not start within {TotalSeconds} seconds",
					timeout.TotalSeconds);
				// Set the waitForDevServer flag to false.
				this._waitForDevServer = false;
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
			this._logger.LogWarning("{Message}. Make sure the Vite development server is running", exp.Message);
			await next(context);
		}
	}
}