// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.Extensions.Options;

namespace Vite.AspNetCore;

/// <summary>
/// Provides information about the Vite development server.
/// </summary>
class ViteDevServerStatus : IViteDevServerStatus
{
    internal static bool IsEnabled { get; set; }
    internal static bool IsMiddlewareEnable { get; set; }
    private readonly ViteOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViteDevServerStatus"/>.
    /// </summary>
    /// <param name="options">Options to configure the Vite development server.</param>
    /// <param name="devServerLauncher">The Vite Development Server launcher.</param>
    public ViteDevServerStatus(
        IOptions<ViteOptions> options,
        ViteDevServerLauncher devServerLauncher
    )
    {
        this.options = options.Value;

        // Make sure the Vite Development Server is running if AutoRun is true.
        if (IsEnabled && this.options.Server.AutoRun)
        {
            devServerLauncher.LaunchIfNotRunning();
        }
    }

    /// <inheritdoc/>
    bool IViteDevServerStatus.IsEnabled => IsEnabled;

    /// <inheritdoc/>
    bool IViteDevServerStatus.IsMiddlewareEnable => IsMiddlewareEnable;

    /// <inheritdoc/>
    string IViteDevServerStatus.ServerUrl => this.options.GetViteDevServerUrl();

    /// <inheritdoc/>
    string IViteDevServerStatus.ServerUrlWithBasePath =>
        this.options.GetViteDevServerUrlWithBasePath();

    /// <inheritdoc/>
    string IViteDevServerStatus.BasePath => this.options.GetViteBasePath();
}
