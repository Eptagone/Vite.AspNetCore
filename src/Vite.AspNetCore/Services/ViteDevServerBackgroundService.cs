using CliWrap;
using CliWrap.EventStream;
using CliWrap.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Vite.AspNetCore.Services;

internal sealed class ViteDevServerBackgroundService : BackgroundService
{
    private readonly ILogger<ViteDevServerBackgroundService> logger;
    private readonly IWebHostEnvironment environment;
    private readonly ViteOptions options;
    
    /// <summary>
    /// Initialize a new instance of <see cref="ViteDevServerBackgroundService"/>.
    /// </summary>
    /// <param name="logger">The logging service.</param>
    /// <param name="options">Options to configure the Vite development server.</param>
    /// <param name="environment">The environment.</param>
    public ViteDevServerBackgroundService(ILogger<ViteDevServerBackgroundService> logger, IOptions<ViteOptions> options, IWebHostEnvironment environment)
    {
        this.logger = logger;
        this.environment = environment;
        this.options = options.Value;
    }

    /// <inheritdoc/>
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Stopping the vite development server...");
        
        return base.StopAsync(cancellationToken);
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Start the Vite development server if AutoRun is true.
        if (!this.options.Server.AutoRun)
        {
            return;
        }
        
        // Set the working directory.
        var workingDirectory = this.options.PackageDirectory ?? this.environment.ContentRootPath;

        // If the working directory is relative, combine it with the app's base directory.
        if (!Path.IsPathRooted(workingDirectory))
        {
            workingDirectory = Path.GetFullPath(workingDirectory, this.environment.ContentRootPath);
        }

        this.logger.LogInformation("Starting the Vite development server...");

        var viteExecutablePath = Path.Combine(workingDirectory, "node_modules/.bin/vite");
        if (OperatingSystem.IsWindows())
        {
            viteExecutablePath += ".CMD";
        }
        
        var cmd = Cli.Wrap(viteExecutablePath).WithWorkingDirectory(workingDirectory);

        try
        {
            await foreach (var cmdEvent in cmd.ListenAsync(stoppingToken))
            {
                switch (cmdEvent)
                {
                    case StandardOutputCommandEvent outputEvent:
                        this.LogViteText(LogLevel.Information, outputEvent.Text);
                        break;
                    case StandardErrorCommandEvent errorEvent:
                        // We log this as a warning as warnings in the Node process are sent through the standard error pipe.
                        this.LogViteText(LogLevel.Warning, errorEvent.Text);
                        break;
                    case ExitedCommandEvent exitedEvent:
                        this.logger.LogError(
                            "Vite development server exited unexpectedly with exit code {exitCode}.",
                            exitedEvent.ExitCode);
                        break;
                }
            }
        }
        catch (CommandExecutionException ex)
        {
            this.logger.LogError(ex, "Failed to start Vite development server. Ensure that 'vite' is listed as a dependency in your package.json and that you have run 'npm install' or the equivalent for your package manager.");
        }
    }

    /// <summary>
    /// Log output from the Vite process.
    /// </summary>
    /// <param name="logLevel">The log level for the text to log.</param>
    /// <param name="text">The text to log.</param>
    private void LogViteText(LogLevel logLevel, string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            this.logger.Log(logLevel, "Vite: {output}", text);
        }
    }
}
