// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;

namespace Vite.AspNetCore.Services;

/// <summary>
/// Represents a middleware that proxies requests to the Vite Dev Server.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ViteDevMiddleware"/> class.
/// </remarks>
/// <param name="logger">The logger service.</param>
/// <param name="devServerStatus">The <see cref="IViteDevServerStatus"/> instance.</param>
/// <param name="clientFactory">The <see cref="IHttpClientFactory"/> instance.</param>
internal class ViteDevMiddleware(
    ILogger<ViteDevMiddleware> logger,
    IViteDevServerStatus devServerStatus,
    IHttpClientFactory clientFactory
) : IMiddleware
{
    private readonly ILogger<ViteDevMiddleware> logger = logger;
    private readonly IHttpClientFactory clientFactory = clientFactory;
    private readonly IViteDevServerStatus devServerStatus = devServerStatus;

    /// <inheritdoc />
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // If the request doesn't have an endpoint, the request path is not null and the request method is GET, proxy the request to the Vite Development Server.
        if (
            context.GetEndpoint() == null
            && context.Request.Path.HasValue
            && context.Request.Method == HttpMethod.Get.Method
        )
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
        using var client = this.clientFactory.CreateClient(ViteDevServerStatus.HttpClientName);

        // Pass "Accept" header from the original request.
        if (context.Request.Headers.ContainsKey("Accept"))
        {
            client.DefaultRequestHeaders.Add("Accept", context.Request.Headers.Accept.ToList());
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
            this.logger.LogWarning(
                "{Message}. Make sure the Vite development server is running",
                exp.Message
            );
            await next(context);
        }
    }
}
