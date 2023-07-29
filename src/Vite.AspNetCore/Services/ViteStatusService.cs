// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

namespace Vite.AspNetCore.Services;

/// <summary>
/// This class provides information about the status of the Vite Development Server.
/// </summary>
public sealed class ViteStatusService
{
	/// <summary>
	/// This property is used to determine if the Vite Development Server is expected to be running.
	/// </summary>
	public static bool IsDevServerRunning { get; internal set; } = false;

	internal static bool IsMiddlewareRegistered { get; set; } = false;
	internal bool IsDevScriptInserted { get; set; }

	public ViteStatusService()
	{
		this.IsDevScriptInserted = false;
	}
}
