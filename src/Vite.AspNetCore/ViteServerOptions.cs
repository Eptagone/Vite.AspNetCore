// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

namespace Vite.AspNetCore;

/// <summary>
/// Options for the Vite Dev Server.
/// </summary>
public record ViteServerOptions
{
	public const string Server = "Server";

	/// <summary>
	/// Enable or disable the automatic start of the Vite Dev Server. Default value is "false".
	/// </summary>
	public bool AutoRun { get; set; } = false;

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
	/// If true, the middleware will use HTTPS to connect to the Vite Dev Server. Default value is "false".
	/// </summary>
	public bool Https { get; set; }

	/// <summary>
	/// Set true to use the full development server URL instead of just the entry points path when using Tag helpers.
	/// Default value is "true".
	/// </summary>
	/// <remarks>
	/// This property is obsolete and it does not have any effect anymore. It will be removed in a future version.
	/// </remarks>
	[Obsolete("This property is obsolete and it does not have any effect anymore. It will be removed in a future version.")]
	public bool UseFullDevUrl { get; set; } = false;
}
