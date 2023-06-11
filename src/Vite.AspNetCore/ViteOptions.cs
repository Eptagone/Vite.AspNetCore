// Copyright (c) 2023 Quetzal Rivera.
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
	public string Manifest { get; init; } = "manifest.json";

	/// <summary>
	/// The subfolder where your assets will be located, including the manifest file.
	/// This value is relative to the web root path.
	/// </summary>
	public string? Base { get; init; }

	/// <summary>
	/// The name of the package manager to use. Default value is "npm".
	/// </summary>
	public string PackageManager { get; init; } = "npm";

	/// <summary>
	/// Options for the Vite Dev Server.
	/// </summary>
	public ViteServerOptions Server { get; init; } = new ViteServerOptions();
}
