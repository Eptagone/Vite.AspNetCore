// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net;
using Vite.AspNetCore.Extensions;

namespace Vite.AspNetCore.Services;

/// <summary>
/// Provides information about the Vite development server.
/// </summary>
class ViteDevServerStatus : IViteDevServerStatus, IDisposable
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

		// If the Vite development server is not enabled, return.
		if (!IsEnabled)
		{
			return;
		}

		// Start the Vite development server if AutoRun is true.
		if (options.Value.Server.AutoRun)
		{
			this.LaunchDevelopmentServer();
			// Ensure the Vite server is stopped when the app shuts down.
			appLifetime.ApplicationStopping.Register(() => this.Dispose(true));
		}

		// Wait for the Vite development server to start or timeout.
		var attempts = options.Value.Server.TimeOut;
		var clientScriptUrl = $"{this.serverUrlWithBasePath}/@vite/client";
		var isRunning = false;

		do
		{
			// Check if the Vite development server is running.
			isRunning = this.IsViteDevelopmentServerRunning().GetAwaiter().GetResult();
			// If it's not running, wait 1 second and try again.
			if (!isRunning)
			{
				Task.Delay(1000).GetAwaiter().GetResult();
			}

			attempts--;
		} while (!isRunning && attempts > 0);

		if (!isRunning)
		{
			logger.LogWarning("The Vite development server did not start within {TotalSeconds} seconds", options.Value.Server.TimeOut);
		}
		else if (options.Value.Server.AutoRun)
		{
			logger.LogInformation("The Vite development server is running at {ServerUrl}", this.serverUrl);
		}

		ViteStatusService.IsDevServerRunning = isRunning;
	}

	/// <inheritdoc/>
	bool IViteDevServerStatus.IsEnabled => IsEnabled;

	/// <inheritdoc/>
	bool IViteDevServerStatus.IsMiddlewareEnable => IsMiddlewareEnable;

	/// <inheritdoc/>
	string IViteDevServerStatus.ServerUrl => this.serverUrl;

	/// <inheritdoc/>
	string IViteDevServerStatus.ServerUrlWithBasePath => this.serverUrlWithBasePath;

	//* Based on <see href="https://github.com/dotnet/aspnetcore/blob/1121b2bbb123ad9044d593955e7a2ef863fcf2e5/src/Middleware/Spa/SpaProxy/src/SpaProxyLaunchManager.cs">Microsoft.AspNetCore.SpaProxy.SpaProxyLaunchManager</see>.
	#region Vite Development Server Launch Manager
	private bool disposedValue;
	private Process? process;
	private Task? launchTask;

	/// <summary>
	/// Start the Vite development server using the specified options.
	/// </summary>
	public void LaunchDevelopmentServer()
	{
		this.launchTask = this.StartViteDevServerIfNotRunningAsync();
	}

	/// <summary>
	/// Start the Vite development server if it is not already running.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns></returns>
	private async Task StartViteDevServerIfNotRunningAsync()
	{
		// If the Vite development server is already running, return.
		if (await this.IsViteDevelopmentServerRunning())
		{
			this.logger.LogInformation("Looks like the Vite development server is already running at {Url}.", this.serverUrl);
			return;
		}

		// Set the command to run.
		var command = this.options.PackageManager;
		// Set the arguments to run.
		var args = $"run {this.options.Server.ScriptName}";
		// Set the working directory.
		var workingDirectory = this.options.PackageDirectory ?? this.environment.ContentRootPath;

		// If the working directory is relative, combine it with the app's base directory.
		if (!Path.IsPathRooted(workingDirectory))
		{
			workingDirectory = Path.GetFullPath(workingDirectory, this.environment.ContentRootPath);
		}

		// Create the process start info.
		var startInfo = new ProcessStartInfo(command, args)
		{
			CreateNoWindow = false,
			UseShellExecute = true,
			WindowStyle = ProcessWindowStyle.Normal,
			WorkingDirectory = Path.GetFullPath(workingDirectory)
		};

		try
		{
			this.logger.LogInformation("Starting the vite development server...");

			// Start the process.
			this.process = Process.Start(startInfo);

			if (this.process is { HasExited: false })
			{
				this.logger.LogInformation("Vite development server started with process ID {ProcessId}.", this.process.Id);

				bool? stopScriptLaunched = null;
				// Ensure the process is killed if the app shuts down.
				if (OperatingSystem.IsWindows())
				{
					stopScriptLaunched = this.LaunchStopScriptForWindows(this.process.Id);
				}
				else if (OperatingSystem.IsMacOS())
				{
					stopScriptLaunched = this.LaunchStopScriptForMacOs(this.process.Id);
				}

				// If the stop script was not launched, log a warning.
				if (stopScriptLaunched == false)
				{
					this.logger.LogError("Failed to launch stop script for Vite development server with process ID {ProcessId}.", this.process.Id);
				}
			}
		}
		catch (Exception exp)
		{
			this.logger.LogError(exp, "Failed to launch Vite development server.");
		}
	}

	/// <summary>
	/// Check if the Vite development server is already running.
	/// </summary>
	/// <param name="url">The Vite development server url.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	private async Task<bool> IsViteDevelopmentServerRunning()
	{
		using var timeout = new CancellationTokenSource(
			TimeSpan.FromMinutes(this.options.Server.TimeOut)
		);
		using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
			timeout.Token,
			this.appLifetime.ApplicationStopping
		);

		try
		{
			// Create the HttpClient.
			var httpClient = this.httpClientFactory.CreateClient(HttpClientName);
			// Test the connection to the Vite development server.
			var response = await httpClient.GetAsync(this.serverUrl, cancellationTokenSource.Token);
			// Check if the Vite development server is running. It could be running if the response is successful or if the status code is 404 (Not Found).
			var running = response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound;
			// Return true if the Vite development server is running, otherwise false.
			return running;
		}
		catch (Exception exception) when (exception is HttpRequestException ||
			  exception is TaskCanceledException ||
			  exception is OperationCanceledException)
		{
			this.logger.LogDebug(exception, "The Vite development server is not running yet.");
			return false;
		}
	}

	/// <summary>
	/// On Windows, kill the process tree using a PowerShell script.
	/// </summary>
	/// <param name="processId">The process ID.</param>
	/// <returns>True if the script was launched successfully, otherwise false.</returns>
	private bool LaunchStopScriptForWindows(int processId)
	{
		// Create the PowerShell script.
		var stopScript =
			$@"do{{
  try
  {{
    $processId = Get-Process -PID {Environment.ProcessId} -ErrorAction Stop;
  }}catch
  {{
    $processId = $null;
  }}
  Start-Sleep -Seconds 1;
}}while($processId -ne $null);
try
{{
  taskkill /T /F /PID {processId};
}}
catch
{{
}}";
		// Define the process start info.
		var stopScriptInfo = new ProcessStartInfo("powershell.exe", string.Join(" ", "-NoProfile", "-C", stopScript))
		{
			CreateNoWindow = true,
			WorkingDirectory = this.environment.ContentRootPath
		};
		// Start the process.
		var stopProcess = Process.Start(stopScriptInfo);

		// Return true if the process was started successfully.
		return !(stopProcess == null || stopProcess.HasExited);
	}

	/// <summary>
	/// On Mac OS, kill the process tree using a Bash script.
	/// </summary>
	/// <param name="processId">The process ID.</param>
	/// <returns>True if the script was launched successfully, otherwise false.</returns>
	private bool LaunchStopScriptForMacOs(int processId)
	{
		// Define the script file name.
		var fileName = Guid.NewGuid().ToString("N") + ".sh";
		// Define the script path.
		var scriptPath = Path.Combine(this.environment.ContentRootPath, fileName);
		// Create the Bash script.
		var stopScript =
			@$"function list_child_processes () {{
    local ppid=$1;
    local current_children=$(pgrep -P $ppid);
    local local_child;
    if [ $? -eq 0 ];
    then
        for current_child in $current_children
        do
          local_child=$current_child;
          list_child_processes $local_child;
          echo $local_child;
        done;
    else
      return 0;
    fi;
}}
ps {Environment.ProcessId};
while [ $? -eq 0 ];
do
  sleep 1;
  ps {Environment.ProcessId} > /dev/null;
done;
for child in $(list_child_processes {processId});
do
  echo killing $child;
  kill -s KILL $child;
done;
rm {scriptPath};
";
		// Write the script to the file.
		File.WriteAllText(scriptPath, stopScript.ReplaceLineEndings());
		// Create the process start info.
		var stopScriptInfo = new ProcessStartInfo("/bin/bash", scriptPath)
		{
			CreateNoWindow = true,
			WorkingDirectory = this.environment.ContentRootPath
		};
		// Start the process.
		var stopProcess = Process.Start(stopScriptInfo);

		// Return true if the process was started successfully.
		return !(stopProcess == null || stopProcess.HasExited);
	}

	private void Dispose(bool disposing)
	{
		if (!this.disposedValue)
		{
			try
			{
				if (this.process?.HasExited is false && this.process?.CloseMainWindow() == false)
				{
					this.process.Kill(true);
					this.process = null;
					this.launchTask?.Dispose();
					this.launchTask = null;
				}
			}
			catch (Exception)
			{
				if (disposing)
				{
					throw;
				}
			}

			// TODO: free unmanaged resources (unmanaged objects) and override finalizer
			// TODO: set large fields to null
			this.disposedValue = true;
		}
	}

	// TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
	~ViteDevServerStatus()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		this.Dispose(disposing: false);
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		this.Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
	#endregion
}
