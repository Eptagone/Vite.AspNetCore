// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

namespace Vite.AspNetCore;

/// <summary>
/// Options for Vite.
/// </summary>
public class ViteOptions
{
	public const string Vite = "Vite";

	/// <summary>
	/// The manifest file name. Default is "manifest.json".
	/// </summary>
	public string Manifest { get; set; } = "manifest.json";
	/// <summary>
	/// The name of the package manager to use. Default value is "npm".
	/// </summary>
	public string PackageManager { get; set; } = "npm";
	/// <summary>
	/// The working directory where your package.json file is located. Default value is the content root path.
	/// </summary>
	public string? WorkingDirectory { get; set; }
	/// <summary>
	/// Options for the Vite Dev Server.
	/// </summary>
	public ViteServerOptions Server { get; set; } = new ViteServerOptions();
}
