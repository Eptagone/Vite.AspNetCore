// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

namespace Vite.AspNetCore;

/// <summary>
/// Provides information about the Vite development server.
/// </summary>
public interface IViteDevServerStatus
{
	/// <summary>
	/// True if the development server is enabled, otherwise false.
	/// </summary>
	public bool IsEnabled { get; }

	/// <summary>
	/// True if the middleware is enabled, otherwise false.
	/// </summary>
	public bool IsMiddlewareEnable { get; }

	/// <summary>
	/// The URL of the Vite development server.
	/// </summary>
	public string ServerUrl { get; }

	/// <summary>
	/// The URL of the Vite development server with base path.
	/// </summary>
	public string ServerUrlWithBasePath { get; }
}
