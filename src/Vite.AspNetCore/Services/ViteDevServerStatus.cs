// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vite.AspNetCore.Extensions;

namespace Vite.AspNetCore.Services;

/// <summary>
/// Provides information about the Vite development server.
/// </summary>
class ViteDevServerStatus : IViteDevServerStatus
{
	internal const string HttpClientName = "Vite.AspNetCore.DevHttpClient";

	internal static bool IsEnabled { get; set; }
	internal static bool IsMiddlewareEnable { get; set; }
	private readonly string serverUrl;
	private readonly string serverUrlWithBasePath;
	private readonly ViteOptions options;
	private readonly ILogger<ViteDevServerStatus> logger;
	private readonly IWebHostEnvironment environment;
	private readonly IHostApplicationLifetime appLifetime;
	private readonly IHttpClientFactory httpClientFactory;

	/// <summary>
	/// Initialize a new instance of <see cref="ViteDevServerStatus"/>.
	/// </summary>
	/// <param name="logger">The logging service.</param>
	/// <param name="options">Options to configure the Vite development server.</param>
	/// <param name="clientFactory">A <see cref="IHttpClientFactory"/> to create <see cref="HttpClient"/> instances.</param>
	[Obsolete]
	public ViteDevServerStatus(ILogger<ViteDevServerStatus> logger, IOptions<ViteOptions> options, IHttpClientFactory clientFactory, IWebHostEnvironment environment, IHostApplicationLifetime appLifetime)
	{
		this.serverUrl = options.Value.GetViteDevServerUrl();
		var basePath = options.Value.Base?.Trim('/');
		this.serverUrlWithBasePath = string.IsNullOrEmpty(basePath) ? this.serverUrl : $"{this.serverUrl}/{basePath}";
		this.logger = logger;
		this.options = options.Value;
		this.environment = environment;
		this.appLifetime = appLifetime;
		this.httpClientFactory = clientFactory;
	}

	/// <inheritdoc/>
	bool IViteDevServerStatus.IsEnabled => IsEnabled;

	/// <inheritdoc/>
	bool IViteDevServerStatus.IsMiddlewareEnable => IsMiddlewareEnable;

	/// <inheritdoc/>
	string IViteDevServerStatus.ServerUrl => this.serverUrl;

	/// <inheritdoc/>
	string IViteDevServerStatus.ServerUrlWithBasePath => this.serverUrlWithBasePath;
}
