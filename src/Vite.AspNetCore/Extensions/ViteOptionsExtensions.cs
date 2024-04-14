// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

namespace Vite.AspNetCore;

/// <summary>
/// Define extension methods for <see cref="ViteOptions"/>
/// </summary>
internal static class ViteOptionsExtensions
{
    internal static string GetViteDevServerUrl(this ViteOptions options)
    {
        // Get the port and host from the configuration.
        var host = options.Server.Host;
        var port = options.Server.Port;
        // Check if https is enabled.
        var https = options.Server.Https;

        var serverUrl = $"{(https ? "https" : "http")}://{host}";
        if (port is not null)
        {
            serverUrl += $":{port}";
        }

        // Return the url.
        return serverUrl;
    }

    internal static string GetViteDevServerUrlWithBasePath(this ViteOptions options)
    {
        var serverUrl = options.GetViteDevServerUrl();
        var basePath = options.GetViteBasePath();
        return $"{serverUrl}{basePath}";
    }

    internal static string GetViteBasePath(this ViteOptions options) {
        var basePath = options.Base?.Trim('/');
        return string.IsNullOrEmpty(basePath)
            ? string.Empty
            : $"/{basePath}";
    }
}
