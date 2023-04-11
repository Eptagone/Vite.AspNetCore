﻿// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.Extensions.Logging;
using System.Text;

namespace Vite.AspNetCore.Utilities
{
	/// <summary>
	/// Capture the output of a node process and send it to the logger.
	/// Inspired by the <a href="https://github.com/dotnet/aspnetcore/blob/main/src/Middleware/Spa/SpaServices.Extensions/src/Util/EventedStreamReader.cs">EventedStreamReader</a> class from the <b>Microsoft.AspNetCore.SpaServices.Extensions</b>.
	/// </summary>
	internal class NodeStreamReader
	{
		private readonly ILogger _logger;
		private readonly StreamReader _streamReader;
		private readonly StringBuilder _linesBuffer;

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
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			// Save the stream reader.
			_streamReader = streamReader ?? throw new ArgumentNullException(nameof(streamReader));
			// Initialize the lines buffer.
			_linesBuffer = new StringBuilder();
			Task.Factory.StartNew(Run, cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		}

		private async Task Run()
		{
			// Create a new buffer.
			var buffer = new char[8 * 1024];
			// Run the loop.
			while (true)
			{
				var chunkLength = await _streamReader.ReadAsync(buffer, 0, buffer.Length);
				if (chunkLength == 0)
				{
					if (_linesBuffer.Length > 0)
					{
						OnCompleteLine(_linesBuffer.ToString());
						_linesBuffer.Clear();
					}

					break;
				}

				int lineBreakPos;
				var startPos = 0;

				// Get all the newlines
				while ((lineBreakPos = Array.IndexOf(buffer, '\n', startPos, chunkLength - startPos)) >= 0 && startPos < chunkLength)
				{
					var length = (lineBreakPos + 1) - startPos;
					_linesBuffer.Append(buffer, startPos, length);
					OnCompleteLine(_linesBuffer.ToString());
					_linesBuffer.Clear();
					startPos = lineBreakPos + 1;
				}

				// Get the rest
				if (lineBreakPos < 0 && startPos < chunkLength)
				{
					_linesBuffer.Append(buffer, startPos, chunkLength - startPos);
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
			}
		}
	}
}
