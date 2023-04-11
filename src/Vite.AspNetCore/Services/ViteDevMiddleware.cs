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
        private readonly ILogger<ViteDevMiddleware> _logger;
        private readonly string _viteServerBaseUrl;
        private readonly NodeScriptRunner? _scriptRunner;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViteDevMiddleware"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger{ViteDevMiddleware}"/> instance.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> instance.</param>
        /// <param name="environment">The <see cref="IWebHostEnvironment"/> instance.</param>
		public ViteDevMiddleware(ILogger<ViteDevMiddleware> logger, IConfiguration configuration, IWebHostEnvironment environment)
        {
            // Set the logger.
            this._logger = logger;
            // Read the Vite options from the configuration.
            var viteOptions = configuration.GetSection(ViteOptions.Vite).Get<ViteOptions>();
            // Get the port from the configuration.
            var port = viteOptions.Server.Port;
            // Check if https is enabled.
            var https = viteOptions.Server.Https;
            // Build the base url.
            this._viteServerBaseUrl = $"{(https ? "https" : "http")}://localhost:{port}";

            // Prepare and run the Vite Dev Server if AutoRun is true.
            if (viteOptions.Server.AutoRun)
            {
                // Gets the package manager command.
                var pkgManagerCommand = viteOptions.PackageManager;
                // Gets the working directory.
                var workingDirectory = viteOptions.WorkingDirectory ?? environment.ContentRootPath;
                // Gets the script name.= to run the Vite Dev Server.
                var scriptName = viteOptions.Server.ScriptName;
                // Create a new instance of the NodeScriptRunner class.
                var waiting = new TimeSpan(0);
                this._scriptRunner = new NodeScriptRunner(logger, pkgManagerCommand, scriptName, workingDirectory,
                    line =>
                    {
                        if (!line.Contains(this._viteServerBaseUrl))
                        {
                            return;
                        }
                        logger.LogInformation("Found Vite Server: {ViteServerBaseUrl}.", this._viteServerBaseUrl);
                        waiting = TimeSpan.MaxValue;
                    });

                logger.LogInformation(
                    "The middleware has called the dev script. It may take a few seconds before the Vite Dev Server becomes available.");

                // wait for the server to start, based on seconds from options
                // the default value is 3 seconds
                // limit to between 0 and 60 seconds (don't hang forever)
                var timeout = TimeSpan.FromSeconds(Math.Clamp(viteOptions.Server.TimeoutInSeconds, 0, 60));
                var increment = TimeSpan.FromMilliseconds(250);
                while (waiting < timeout) {
                    waiting = waiting.Add(increment);
                    Thread.Sleep(increment);
                }
            }
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
            using HttpClient client = new() { BaseAddress = new Uri(this._viteServerBaseUrl) };

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
                this._logger.LogWarning("{Message}. Make sure Vite Dev Server is running.", exp.Message);
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
}
