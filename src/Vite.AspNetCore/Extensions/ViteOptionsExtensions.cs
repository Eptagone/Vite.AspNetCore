// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Vite.AspNetCore.Extensions;

/// <summary>
/// Define extension methods for <see cref="ViteOptions"/>
/// </summary>
internal static class ViteOptionsExtensions
{
	/// <summary>
	/// Returns the Vite Dev Server Url.
	/// </summary>
	/// <param name="options"></param>
	/// <returns></returns>
	internal static string GetViteDevServerUrl(this ViteOptions options)
	{
		// Get the port and host from the configuration.
		var host = options.Server.Host;
		var port = options.Server.Port;
		// Check if https is enabled.
		var https = options.Server.Https;
		
		// Return the url.
		return $"{(https ? "https" : "http")}://{host}:{port}";
	}
}
