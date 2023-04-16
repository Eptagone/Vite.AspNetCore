// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Vite.AspNetCore.Utilities;

/// <summary>
/// This class is used to run Node scripts. Inspired by the <a href="https://github.com/dotnet/aspnetcore/blob/main/src/Middleware/Spa/SpaServices.Extensions/src/Npm/NodeScriptRunner.cs">NodeScriptRunner</a> class from the <b>Microsoft.AspNetCore.SpaServices.Extensions</b> package.
/// </summary>
internal sealed class NodeScriptRunner : IDisposable
{
	private readonly ILogger _logger;
	private readonly Process? _npmProcess;
	private readonly NodeStreamReader _stdOutReader;
	private readonly NodeStreamReader _stdErrorReader;

	/// <summary>
	/// Initializes a new instance of the <see cref="NodeScriptRunner"/> class.
	/// </summary>
	public NodeScriptRunner(ILogger logger, string pkgManagerCommand, string scriptName, string workingDirectory, CancellationToken cancellationToken = default)
	{
		this._logger = logger;
		// If the package manager command is null or empty, throw an exception.
		if (string.IsNullOrEmpty(pkgManagerCommand))
		{
			throw new ArgumentNullException(nameof(pkgManagerCommand), "The package manager command cannot be null or empty.");
		}
		// If the script name is null or empty, throw an exception.
		if (string.IsNullOrEmpty(scriptName))
		{
			throw new ArgumentNullException(nameof(scriptName), "The script name cannot be null or empty.");
		}
		// If the working directory is null or empty, throw an exception.
		if (string.IsNullOrEmpty(workingDirectory))
		{
			throw new ArgumentNullException(nameof(workingDirectory), "The working directory cannot be null or empty.");
		}

		// Set the command to run.
		var exeToRun = pkgManagerCommand;
		// Set the arguments.
		var args = $"run {scriptName}";

		// If the OS is Windows, use cmd.exe to run the node executable is cmd file.
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			exeToRun = "cmd";
			args = $"/c {pkgManagerCommand} {args}";
		}

		// Set the process start info.
		var psi = new ProcessStartInfo(exeToRun)
		{
			Arguments = args,
			WorkingDirectory = workingDirectory,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
		};

		// Try to start the process.
		try
		{
			// Create a new process.
			var process = Process.Start(psi) ?? throw new InvalidOperationException($"Unable to start the process '{exeToRun} {args}'.");
			process.EnableRaisingEvents = true;
			// Save the process.
			this._npmProcess = process;
		}
		// If an exception is thrown, throw a new exception.
		catch (Exception ex)
		{
			var message = $"Unable to start the process '{pkgManagerCommand}'. Make sure the package manager command is installed and available in the PATH.";
			throw new InvalidOperationException(message, ex);
		}

		// Create the stream readers.
		this._stdOutReader = new NodeStreamReader(logger, this._npmProcess.StandardOutput, cancellationToken);
		this._stdErrorReader = new NodeStreamReader(logger, this._npmProcess.StandardError, cancellationToken: cancellationToken);

		cancellationToken.Register(((IDisposable)this).Dispose);
	}

	/// <summary>
	/// The standard output reader.
	/// </summary>
	public NodeStreamReader StdOutReader => this._stdOutReader;

	/// <summary>
	/// The standard error reader.
	/// </summary>

	void IDisposable.Dispose()
	{
		// If the process is not null, dispose it.
		if (this._npmProcess != null && !this._npmProcess.HasExited)
		{
			this._npmProcess.Kill(entireProcessTree: true);
			this._npmProcess.Dispose();
		}
	}
}
