// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
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
        private readonly ViteOptions _viteOptions;

        // Waiting for dev server logic
        private bool _viteServerFound;
        // the initialization task (which is null after it is executed once)
        private Task? _initializationTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViteDevMiddleware"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger{ViteDevMiddleware}"/> instance.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> instance.</param>
        /// <param name="environment">The <see cref="IWebHostEnvironment"/> instance.</param>
        /// <param name="lifetime">The <see cref="IHostApplicationLifetime"/> instance.</param>
        public ViteDevMiddleware(ILogger<ViteDevMiddleware> logger, IConfiguration configuration, IWebHostEnvironment environment, IHostApplicationLifetime lifetime)
        {
            // Set the logger.
            this._logger = logger;
            // Read the Vite options from the configuration.
            this._viteOptions = configuration.GetSection(ViteOptions.Vite).Get<ViteOptions>();
            // Get the port from the configuration.
            var port = _viteOptions.Server.Port;
            // Check if https is enabled.
            var https = _viteOptions.Server.Https;
            // Build the base url.
            this._viteServerBaseUrl = $"{(https ? "https" : "http")}://localhost:{port}";
            
            // Prepare and run the Vite Dev Server if AutoRun is true.
            if (_viteOptions.Server.AutoRun)
            {
                // Gets the package manager command.
                var pkgManagerCommand = _viteOptions.PackageManager;
                // Gets the working directory.
                var workingDirectory = _viteOptions.WorkingDirectory ?? environment.ContentRootPath;
                // Gets the script name.= to run the Vite Dev Server.
                var scriptName = _viteOptions.Server.ScriptName;
                // Create a new instance of the NodeScriptRunner class.
                this._scriptRunner = new NodeScriptRunner(logger, pkgManagerCommand, scriptName, workingDirectory,
                    line =>
                    {
                        if (!line.Contains(this._viteServerBaseUrl))
                        {
                            return;
                        }
                        logger.LogInformation("Found Vite Server: {ViteServerBaseUrl}.", this._viteServerBaseUrl);
                        this._viteServerFound = true;
                    });

                logger.LogInformation(
                    "The middleware has called the dev script. It may take a few seconds before the Vite Dev Server becomes available.");

                // register initialization task with the application's lifetime
                var startRegistration = default(CancellationTokenRegistration);
                lifetime.ApplicationStarted.Register(() =>
                {
                    this._initializationTask = this.InitializeAsync(lifetime.ApplicationStopping);
                    startRegistration.Dispose();
                });
            }
        }

        /// <summary>
        /// Add any initialization steps here in the order they need to execute
        /// </summary>
        /// <param name="cancellationToken"></param>
        private async Task InitializeAsync(CancellationToken cancellationToken)
        {
            // Add middleware initialization tasks here
            await this.WaitForViteDevServer(cancellationToken);
        }

        /// <summary>
        /// Waits for vite dev server using the timeout
        /// </summary>
        /// <param name="cancellationToken"></param>
        private async Task WaitForViteDevServer(CancellationToken cancellationToken)
        {
            // wait for the server to start, based on seconds from options
            // the default value is 3 seconds
            // limit to between 0 and 60 seconds (don't hang forever)
            var timeout = TimeSpan.FromSeconds(Math.Clamp(this._viteOptions.Server.TimeoutInSeconds, 0, 60));
            // smaller increments mean faster discover
            // but potentially more loops
            var increment = TimeSpan.FromMilliseconds(25);
            var waiting = new TimeSpan(0);

            while (!this._viteServerFound && waiting < timeout)
            {
                waiting = waiting.Add(increment);
                await Task.Delay(increment, cancellationToken);
            }

            this._logger.LogInformation("Waited for {TotalMilliSeconds} seconds for Vite dev server",
                waiting.TotalMilliseconds);
        }

        /// <inheritdoc />
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var initializationTask = this._initializationTask;
            if (initializationTask != null)
            {
                // Wait until initialization is complete before passing the request to next middleware
                await initializationTask;

                // Clear the task so that we don't await it again later.
                this._initializationTask = null;
            }

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
            // if we're disposing before the task has run, dispose it
            if (this._initializationTask != null)
            {
                this._initializationTask.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }
}
