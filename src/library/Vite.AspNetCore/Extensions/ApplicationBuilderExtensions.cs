// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.AspNetCore.Builder;

namespace Vite.AspNetCore;

/// <summary>
/// Vite extension methods for <see cref="IApplicationBuilder"/>.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Instructs the Tag Helpers to render urls to the Vite Development Server and adds the <see cref="ViteDevServerMiddleware"/> to the pipeline if it is enabled.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
    /// <param name="useMiddleware">If true, a middleware will be registered to proxy all requests to the Vite Development Server.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> instance this method extends.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IApplicationBuilder UseViteDevelopmentServer(
        this IApplicationBuilder app,
        bool useMiddleware = false
    )
    {
        ArgumentNullException.ThrowIfNull(app);
        ViteDevServerStatus.IsEnabled = true;
        ViteDevServerStatus.IsMiddlewareEnable = useMiddleware;

        return useMiddleware ? app.UseMiddleware<ViteDevServerMiddleware>() : app;
    }
}
