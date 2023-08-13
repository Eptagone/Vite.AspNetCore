// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Vite.AspNetCore.Services;

/// <summary>
/// This class is used to launch the Vite development server.
/// Based on <see href="https://github.com/dotnet/aspnetcore/blob/1121b2bbb123ad9044d593955e7a2ef863fcf2e5/src/Middleware/Spa/SpaProxy/src/SpaProxyLaunchManager.cs">Microsoft.AspNetCore.SpaProxy.SpaProxyLaunchManager</see>.
/// </summary>
internal sealed class ViteServerLaunchManager : IDisposable
{
	private readonly ILogger _logger;
	private readonly IWebHostEnvironment _environment;
	private readonly ViteOptions _options;

	private bool disposedValue;
	private Process? _process;

	/// <summary>
	/// Initialize a new instance of <see cref="ViteServerLaunchManager"/>.
	/// </summary>
	/// <param name="logger">The logging service.</param>
	/// <param name="appLifetime">The application lifetime service.</param>
	/// <param name="options">The Vite options.</param>
	public ViteServerLaunchManager(
		ILogger<ViteServerLaunchManager> logger,
		IOptions<ViteOptions> options,
		IWebHostEnvironment environment,
		IHostApplicationLifetime appLifetime)
	{
		this._logger = logger;
		this._options = options.Value;
		this._environment = environment;

		// Ensure the Vite server is stopped when the app shuts down.
		appLifetime.ApplicationStopping.Register(() => this.Dispose(true));
	}

	/// <summary>
	/// Start the Vite development server using the specified options.
	/// </summary>
	public void LaunchDevelopmentServer()
	{
		// Set the command to run.
		var command = this._options.PackageManager;
		// Set the arguments to run.
		var args = $"run {this._options.Server.ScriptName}";
		// Set the working directory.
		var workingDirectory = this._options.PackageDirectory ?? this._environment.ContentRootPath;

		// If the working directory is relative, combine it with the app's base directory.
		if (!Path.IsPathRooted(workingDirectory))
		{
			workingDirectory = Path.GetFullPath(workingDirectory, this._environment.ContentRootPath);
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
			this._logger.LogInformation("Starting the vite development server...");

			// Start the process.
			this._process = Process.Start(startInfo);

			if (this._process is { HasExited: false })
			{
				this._logger.LogInformation("Vite development server started with process ID {ProcessId}.", this._process.Id);

				bool? stopScriptLaunched = null;
				// Ensure the process is killed if the app shuts down.
				if (OperatingSystem.IsWindows())
				{
					stopScriptLaunched = this.LaunchStopScriptForWindows(this._process.Id);
				}
				else if (OperatingSystem.IsMacOS())
				{
					stopScriptLaunched = this.LaunchStopScriptForMacOs(this._process.Id);
				}

				// If the stop script was not launched, log a warning.
				if (stopScriptLaunched == false)
				{
					this._logger.LogError("Failed to launch stop script for Vite development server with process ID {ProcessId}.", this._process.Id);
				}
			}
		}
		catch (Exception exp)
		{
			this._logger.LogError(exp, "Failed to launch Vite development server.");
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
			WorkingDirectory = this._environment.ContentRootPath
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
		var scriptPath = Path.Combine(this._environment.ContentRootPath, fileName);
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
			WorkingDirectory = this._environment.ContentRootPath
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
				if (this._process?.HasExited is false && this._process?.CloseMainWindow() == false)
				{
					this._process.Kill(true);
					this._process = null;
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

	// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
	~ViteServerLaunchManager()
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
}
