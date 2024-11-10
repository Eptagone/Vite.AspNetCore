// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Vite.AspNetCore;

/// <summary>
/// Provides a way to launch the Vite Development Server.
/// </summary>
/// <remarks>
/// Based on <see href="https://github.com/dotnet/aspnetcore/blob/1121b2bbb123ad9044d593955e7a2ef863fcf2e5/src/Middleware/Spa/SpaProxy/src/SpaProxyLaunchManager.cs">Microsoft.AspNetCore.SpaProxy.SpaProxyLaunchManager</see>
/// </remarks>
/// <param name="logger">An <see cref="ILogger{TCategoryName}"/> instance used to log messages.</param>
/// <param name="options">The Vite options.</param>
/// <param name="environment">The <see cref="IWebHostEnvironment"/> instance.</param>
internal sealed class ViteDevServerLauncher(
    ILogger<ViteDevServerLauncher> logger,
    IOptions<ViteOptions> options,
    IWebHostEnvironment environment,
    IHostApplicationLifetime appLifetime,
    IHttpClientFactory httpClientFactory
) : IDisposable
{
    private readonly ILogger<ViteDevServerLauncher> logger = logger;
    private readonly ViteOptions options = options.Value;
    private readonly string contentRootPath = environment.ContentRootPath;
    private readonly IHostApplicationLifetime appLifetime = appLifetime;
    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
    private bool disposedValue;
    private Process? process;
    private Task? launchTask;
    private bool isRunning;

    /// <summary>
    /// Launch the Vite development server.
    /// </summary>
    public void LaunchIfNotRunning()
    {
        this.launchTask ??= this.StartViteDevServerIfNotRunningAsync();

        if (!this.isRunning)
        {
            // Wait for the Vite development server to start or timeout.
            var attempts = this.options.Server.TimeOut;

            do
            {
                // Check if the Vite development server is running.
                this.IsViteDevelopmentServerRunning().GetAwaiter().GetResult();
                // If it's not running, wait 1 second and try again.
                if (!this.isRunning)
                {
                    Task.Delay(1000).GetAwaiter().GetResult();
                }

                attempts--;
            } while (!this.isRunning && attempts > 0);

            if (!this.isRunning)
            {
                this.logger.LogViteDevServerDidNotStart(this.options.Server.TimeOut);
            }
            else if (this.options.Server.AutoRun)
            {
                this.logger.LogViteDevServerRunning(this.options.GetViteDevServerUrl());
            }
        }
    }

    /// <summary>
    /// Check if the Vite development server is already running.
    /// </summary>
    private async Task<bool> IsViteDevelopmentServerRunning()
    {
        if (this.isRunning)
        {
            return true;
        }

        var httpClient = this.httpClientFactory.CreateClient(ViteDevServerMiddleware.HTTP_CLIENT_NAME);
        using var timeout = new CancellationTokenSource(
            TimeSpan.FromMinutes(this.options.Server.TimeOut)
        );
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            timeout.Token,
            this.appLifetime.ApplicationStopping
        );

        try
        {
            // Test the connection to the Vite development server.
            var response = await httpClient.GetAsync(
                this.options.GetViteDevServerUrl(),
                cancellationTokenSource.Token
            );
            // Check if the Vite development server is running. It could be running if the response is successful or if the status code is 404 (Not Found).
            var running =
                response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound;
            // Return true if the Vite development server is running, otherwise false.
            return this.isRunning = running;
        }
        catch (Exception exception)
            when (exception is HttpRequestException
                || exception is TaskCanceledException
                || exception is OperationCanceledException
            )
        {
            this.logger.LogViteDevServerNotRunning();
            return false;
        }
    }

    /// <summary>
    /// Start the Vite development server if it is not already running.
    /// </summary>
    /// <returns></returns>
    private async Task StartViteDevServerIfNotRunningAsync()
    {
        // If the Vite development server is already running, return.
        if (await this.IsViteDevelopmentServerRunning())
        {
            this.logger.LogViteDevServerAlreadyRunning(this.options.GetViteDevServerUrl());
            return;
        }

        // Set the command to run.
        var command = this.options.Server.PackageManager;
        // Set the arguments to run.
        var args = $"run {this.options.Server.ScriptName}";
        if (!string.IsNullOrWhiteSpace(this.options.Server.ScriptArgs))
        {
            args += $" -- {this.options.Server.ScriptArgs}";
        }
        // Set the working directory.
        var workingDirectory = this.options.Server.PackageDirectory ?? this.contentRootPath;

        // If the working directory is relative, combine it with the app's base directory.
        if (!Path.IsPathRooted(workingDirectory))
        {
            workingDirectory = Path.GetFullPath(workingDirectory, this.contentRootPath);
        }

        // Create the process start info.
        var startInfo = new ProcessStartInfo(command, args)
        {
            CreateNoWindow = false,
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Normal,
            WorkingDirectory = Path.GetFullPath(workingDirectory),
        };

        try
        {
            this.logger.LogStartingViteDevServer();

            // Start the process.
            this.process = Process.Start(startInfo);

            if (this.process is { HasExited: false })
            {
                this.logger.LogViteDevServerStarted(this.process.Id);

                bool? stopScriptLaunched = null;
                // Ensure the process is killed if the app shuts down.
                if (OperatingSystem.IsWindows())
                {
                    ChildProcessTracker.AddProcess(this.process);
                    return;
                }
                else if (OperatingSystem.IsMacOS())
                {
                    stopScriptLaunched = LaunchStopScriptForMacOs(this.process.Id);
                }

                // If the stop script was not launched, log a warning.
                if (stopScriptLaunched == false)
                {
                    this.logger.LogFailedToLaunchStopScript(this.process.Id);
                }
            }
        }
        catch (Exception exp)
        {
            this.logger.LogFailedToLaunchViteDevServer(exp.Message);
        }
    }

    /// <summary>
    /// On Mac OS, kill the process tree using a Bash script.
    /// </summary>
    /// <param name="processId">The process ID.</param>
    /// <returns>True if the script was launched successfully, otherwise false.</returns>
    private static bool LaunchStopScriptForMacOs(int processId)
    {
        // Define the script file name.
        var fileName = "start-vite-dev-server.sh";
        // Define the script path.
        var scriptPath = Path.Combine(AppContext.BaseDirectory, fileName);
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
            WorkingDirectory = AppContext.BaseDirectory,
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
    ~ViteDevServerLauncher()
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
