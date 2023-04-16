// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.RegularExpressions;

namespace Vite.AspNetCore.Utilities;

/// <summary>
/// Capture the output of a node process and send it to the logger.
/// Inspired by the <a href="https://github.com/dotnet/aspnetcore/blob/main/src/Middleware/Spa/SpaServices.Extensions/src/Util/EventedStreamReader.cs">EventedStreamReader</a> class from the <b>Microsoft.AspNetCore.SpaServices.Extensions</b>.
/// </summary>
internal class NodeStreamReader
{
	public delegate void OnReceivedLineHandler(string line);

	private readonly ILogger _logger;
	private readonly StreamReader _streamReader;
	private readonly StringBuilder _linesBuffer;

	private static readonly Regex AnsiColorRegex = new(@"\x1B\[[0-?]*[ -/]*[@-~]", RegexOptions.Compiled);

	public event OnReceivedLineHandler? OnReceivedLine;

	/// <summary>
	/// Initialize a new instance of the <see cref="NodeStreamReader"/> class.
	/// </summary>
	/// <param name="logger">The logger.</param>
	/// <param name="streamReader">The stream reader.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <exception cref="ArgumentNullException"></exception>
	public NodeStreamReader(ILogger logger, StreamReader streamReader, CancellationToken cancellationToken = default)
	{
		// Save the logger.
		this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
		// Save the stream reader.
		this._streamReader = streamReader ?? throw new ArgumentNullException(nameof(streamReader));
		// Initialize the lines buffer.
		this._linesBuffer = new StringBuilder();
		// Start the task.
		Task.Factory.StartNew(this.Run, cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
	}

	private async Task Run()
	{
		// Create a new buffer.
		var buffer = new char[8 * 1024];
		// Run the loop.
		while (true)
		{
			var chunkLength = await this._streamReader.ReadAsync(buffer, 0, buffer.Length);
			if (chunkLength == 0)
			{
				if (this._linesBuffer.Length > 0)
				{
					this.OnCompleteLine(this._linesBuffer.ToString());
					this._linesBuffer.Clear();
				}

				break;
			}

			int lineBreakPos;
			var startPos = 0;

			// Get all the newlines
			while ((lineBreakPos = Array.IndexOf(buffer, '\n', startPos, chunkLength - startPos)) >= 0 && startPos < chunkLength)
			{
				var length = (lineBreakPos + 1) - startPos;
				this._linesBuffer.Append(buffer, startPos, length);
				this.OnCompleteLine(this._linesBuffer.ToString());
				this._linesBuffer.Clear();
				startPos = lineBreakPos + 1;
			}

			// Get the rest
			if (lineBreakPos < 0 && startPos < chunkLength)
			{
				this._linesBuffer.Append(buffer, startPos, chunkLength - startPos);
			}
		}
	}

	/// <summary>
	/// Print the line to the logger and raise the <see cref="OnReceivedLine"/> event.
	/// </summary>
	private void OnCompleteLine(string line)
	{
		if (!string.IsNullOrEmpty(line) && !string.IsNullOrWhiteSpace(line) && !line.StartsWith('>'))
		{
			this._logger.LogInformation("{Line}", line);
			// Remove the ANSI color codes.
			var lineWithoutAnsi = AnsiColorRegex.Replace(line, string.Empty);
			this.OnReceivedLine?.Invoke(lineWithoutAnsi);
		}
	}
}
