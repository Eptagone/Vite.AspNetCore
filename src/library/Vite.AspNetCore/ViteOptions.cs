// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

namespace Vite.AspNetCore;

/// <summary>
/// Options for Vite.
/// </summary>
public record ViteOptions
{
    public const string Vite = "Vite";

    /// <summary>
    /// The manifest file name. Default is "manifest.json".
    /// </summary>
    public string Manifest { get; set; } = Path.Combine(".vite", "manifest.json");

    /// <summary>
    /// The subfolder where your assets will be located, including the manifest file.
    /// This value is relative to the web root path.
    /// </summary>
    public string? Base { get; set; }

    /// <summary>
    /// Options for the Vite Development Server.
    /// </summary>
    public ViteDevServerOptions Server { get; set; } = new();
}
