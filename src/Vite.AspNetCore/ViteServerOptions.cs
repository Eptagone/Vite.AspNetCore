// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

namespace Vite.AspNetCore
{
	/// <summary>
	/// Options for the Vite Dev Server.
	/// </summary>
	public class ViteServerOptions
	{
		public const string Server = "Server";
		/// <summary>
		/// The port where the Vite Dev Server will be running. Default value is "5173".
		/// </summary>
		public ushort Port { get; set; } = 5173;
		/// <summary>
		/// If true, the middleware will use HTTPS to connect to the Vite Dev Server. Default value is "false".
		/// </summary>
		public bool Https { get; set; } = false;
		/// <summary>
		/// Enable or disable the automatic start of the Vite Dev Server. Default value is "true".
		/// </summary>
		public bool AutoRun { get; set; } = true;
		/// <summary>
		/// The script name to run the Vite Dev Server. Default value is "dev".
		/// </summary>
		public string ScriptName { get; set; } = "dev";
	}
}
