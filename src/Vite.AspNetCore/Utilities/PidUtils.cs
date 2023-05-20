// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Vite.AspNetCore.Utilities;

/// <summary>
/// Provides utility methods for working with process IDs and ports.
/// This class is based on the <a href="https://github.com/EEParker/aspnetcore-vueclimiddleware/blob/3ddb5cac717bbf39642b34f9b04b1828616fc5e5/src/VueCliMiddleware/Util/KillPort.cs#L203">PidUtils class from the VueCliMiddleware package</a>.
/// </summary>
internal static class PidUtils
{
	const string ssPidRegex = @"(?:^|"",|"",pid=)(\d+)";

	/// <summary>
	/// Gets the process ID associated with a port.
	/// </summary>
	/// <param name="port">The port number.</param>
	/// <returns>The process ID associated with the port, or -1 if no process was found.</returns>
	internal static int GetPortPid(ushort port)
	{
		var processId = -1;
		var portColumn = 1; // windows
		var processIdColumn = 4; // windows
		string? processIdRegex = null;

		var results = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) switch
		{
			true => RunProcessReturnOutputSplit("netstat", "-anv -p tcp")
				.Concat(RunProcessReturnOutputSplit("netstat", "-anv -p udp"))
				.ToList(),
			_ => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) switch
			{
				true => RunProcessReturnOutputSplit("ss", "-tunlp"),
				_ => RunProcessReturnOutputSplit("netstat", "-ano")
			}
		};

		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			portColumn = 3;
			processIdColumn = 8;
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			portColumn = 4;
			processIdColumn = 6;
			processIdRegex = ssPidRegex;
		}

		foreach (var line in results)
		{
			if (line.Length <= portColumn || line.Length <= processIdColumn) continue;
			var portMatch = Regex.Match(line[portColumn], $"[.:]({port})");
			if (!portMatch.Success) continue;

			if (int.TryParse(portMatch.Groups[1].Value, out var portValue))
			{
				if (processIdRegex == null)
				{
					if (int.TryParse(line[processIdColumn], out processId)) return processId;
				}
				else
				{
					var pidMatch = Regex.Match(line[processIdColumn], processIdRegex);
					if (pidMatch.Success && int.TryParse(pidMatch.Groups[1].Value, out processId)) return processId;
				}
			}
		}

		return processId;
	}

	/// <summary>
	/// Runs a process with the specified arguments and returns its output as a list of string arrays,
	/// where each string array represents the words in a line of the output.
	/// </summary>
	/// <param name="fileName">The name of the process to run.</param>
	/// <param name="arguments">The arguments to pass to the process.</param>
	/// <returns>A list of string arrays representing the words in each line of the process output.</returns>
	private static List<string[]> RunProcessReturnOutputSplit(string fileName, string arguments)
	{
		var result = RunProcessReturnOutput(fileName, arguments) ?? string.Empty;
		var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
		return lines.Select(line => line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)).ToList();
	}

	/// <summary>
	/// Runs a process with the specified arguments and returns its output as a string.
	/// </summary>
	/// <param name="fileName">The name of the process to run.</param>
	/// <param name="arguments">The arguments to pass to the process.</param>
	/// <returns>The output of the process as a string, or null if an error occurred.</returns>
	private static string? RunProcessReturnOutput(string fileName, string arguments)
	{
		try
		{
			using var process = new Process
			{
				StartInfo = new ProcessStartInfo(fileName, arguments)
				{
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true
				}
			};

			process.Start();
			var standardOutputTask = process.StandardOutput.ReadToEndAsync();
			var standardErrorTask = process.StandardError.ReadToEndAsync();

			if (!process.WaitForExit(10000))
			{
				try
				{
					process.Kill();
				}
				catch
				{
					// ignored
				}
			}

			if (Task.WaitAll(new[] { standardOutputTask, standardErrorTask }, 10000))
			{
				return $"{standardOutputTask.Result}{Environment.NewLine}{standardErrorTask}".Trim();
			}

			return null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	/// <summary>
	/// Kills the process associated with a port.
	/// </summary>
	/// <param name="port">The port number.</param>
	/// <returns>True if the process was killed successfully, false otherwise.</returns>
	internal static bool KillPort(ushort port)
	{
		// The process ID of the process to kill.
		var pid = GetPortPid(port);
		if (pid == -1) return false;

		var arguments = new List<string>();
		try
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				arguments.Add("-9");
				arguments.Add(pid.ToString());
				RunProcessReturnOutput("kill", string.Join(" ", arguments));
			}
			else
			{
				arguments.Add("/f");
				arguments.Add("/T");
				arguments.Add("/PID");
				arguments.Add(pid.ToString());
				return RunProcessReturnOutput("taskkill", string.Join(" ", arguments))?.StartsWith("SUCCESS") ?? false;
			}

			return true;
		}
		catch (Exception)
		{
			// ignored
		}

		return false;
	}
}