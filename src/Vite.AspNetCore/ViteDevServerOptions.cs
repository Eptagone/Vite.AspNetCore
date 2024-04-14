// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

namespace Vite.AspNetCore;

/// <summary>
/// Options for the Vite Dev Server.
/// </summary>
public record ViteDevServerOptions
{
    public const string Server = "Server";

    /// <summary>
    /// The port where the Vite Dev Server will be running. Default value is "5173".
    /// </summary>
    /// <remarks>
    /// If the port is null, server URL will be generated without port.
    /// Example: http://localhost
    /// </remarks>
    public ushort? Port { get; set; } = 5173;

    /// <summary>
    /// The host where the Vite Dev Server will be running. Default value is "localhost".
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Wait for Vite Server to load in seconds.
    /// Default value is "5".
    /// </summary>
    public int TimeOut { get; set; } = 5;

    /// <summary>
    /// If true, the middleware will use HTTPS to connect to the Vite Dev Server.
    /// Default value is "false".
    /// </summary>
    public bool Https { get; set; }

    /// <summary>
    /// Inject the react-refresh preamble and enable HMR for React components, see: https://vitejs.dev/guide/backend-integration.html.
    /// Default value is false.
    /// </summary>
    public bool? UseReactRefresh { get; set; }

    #region AutoRun

    /// <summary>
    /// Enable or disable the automatic start of the Vite development server. Default value is "false".
    /// </summary>
    public bool AutoRun { get; set; }

    /// <summary>
    /// The name of the package manager to use to run the Vite development server.
    /// Default value is "npm".
    /// </summary>
    public string PackageManager { get; set; } = "npm";

    /// <summary>
    /// The directory where the package.json file is located.
    /// Default value is the .NET project working directory.
    /// </summary>
    public string? PackageDirectory { get; set; }

    /// <summary>
    /// The script name to run the Vite development server when <see cref="AutoRun"/> is enabled.
    /// Default value is "dev".
    /// </summary>
    public string ScriptName { get; set; } = "dev";

    #endregion
}
