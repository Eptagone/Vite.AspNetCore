// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.Extensions.Logging;

namespace Vite.AspNetCore;

internal static partial class LogMessages
{
    #region ViteManifest
    [LoggerMessage(
        EventId = 1,
        Message = "The manifest file won't be read because the vite development service is enabled. The service will always return null chunks",
        Level = LogLevel.Information
    )]
    internal static partial void LogManifestFileWontBeRead(this ILogger logger);

    [LoggerMessage(
        EventId = 2,
        Message = "Attempted to get the record '{Record}' from the manifest file while the vite development server is enabled. Null was returned",
        Level = LogLevel.Warning
    )]
    internal static partial void LogManifestFileReadAttempt(this ILogger logger, string record);

    [LoggerMessage(
        EventId = 3,
        Message = "Requesting a chunk with the base path included is deprecated. Please remove the base path from the key '{Key}'",
        Level = LogLevel.Warning
    )]
    internal static partial void LogRequestingChunkWithBasePath(this ILogger logger, string key);

    [LoggerMessage(
        EventId = 4,
        Message = "The chunk '{Key}' was not found",
        Level = LogLevel.Warning
    )]
    internal static partial void LogChunkNotFound(this ILogger logger, string key);

    [LoggerMessage(
        EventId = 5,
        Message = "Detected change in Vite manifest - refreshing",
        Level = LogLevel.Information
    )]
    internal static partial void LogDetectedChangeInManifest(this ILogger logger);

    [LoggerMessage(
        EventId = 6,
        Message = "The manifest file was not found. Did you forget to build the assets? ('npm run build')",
        Level = LogLevel.Error
    )]
    internal static partial void LogManifestFileNotFound(this ILogger logger);
    #endregion

    #region Middleware
    [LoggerMessage(
        EventId = 10,
        Message = "{Message}. Make sure the Vite development server is running",
        Level = LogLevel.Warning
    )]
    internal static partial void LogMiddlewareProxyViaHttpError(
        this ILogger logger,
        string message
    );
    #endregion

    #region ViteDevServerLauncher
    [LoggerMessage(
        EventId = 20,
        Message = "Looks like the Vite development server is already running at {ViteDevServerUrl}.",
        Level = LogLevel.Information
    )]
    internal static partial void LogViteDevServerAlreadyRunning(
        this ILogger logger,
        string viteDevServerUrl
    );

    [LoggerMessage(
        EventId = 21,
        Message = "Starting the Vite development server...",
        Level = LogLevel.Information
    )]
    internal static partial void LogStartingViteDevServer(this ILogger logger);

    [LoggerMessage(
        EventId = 22,
        Message = "Vite development server started with process ID {ProcessId}.",
        Level = LogLevel.Information
    )]
    internal static partial void LogViteDevServerStarted(this ILogger logger, int processId);

    [LoggerMessage(
        EventId = 23,
        Message = "Failed to launch stop script for Vite development server with process ID {ProcessId}.",
        Level = LogLevel.Error
    )]
    internal static partial void LogFailedToLaunchStopScript(this ILogger logger, int processId);

    [LoggerMessage(
        EventId = 24,
        Message = "Failed to launch Vite development server. {Message}",
        Level = LogLevel.Error
    )]
    internal static partial void LogFailedToLaunchViteDevServer(
        this ILogger logger,
        string message
    );

    [LoggerMessage(
        EventId = 25,
        Message = "The Vite development server is not running yet.",
        Level = LogLevel.Debug
    )]
    internal static partial void LogViteDevServerNotRunning(this ILogger logger);

    [LoggerMessage(
        EventId = 26,
        Message = "The Vite development server is running at {ServerUrl}",
        Level = LogLevel.Information
    )]
    internal static partial void LogViteDevServerRunning(this ILogger logger, string serverUrl);

    [LoggerMessage(
        EventId = 27,
        Message = "The Vite development server did not start within {TotalSeconds} seconds",
        Level = LogLevel.Warning
    )]
    internal static partial void LogViteDevServerDidNotStart(this ILogger logger, int totalSeconds);

    #endregion

    #region WebSocket proxy
    [LoggerMessage(
        EventId = 30,
        Message = "Establishing HMR WebSocket proxy: {ClientWebSocketUri} -> {TargetWebSocketUri}",
        Level = LogLevel.Information
    )]
    internal static partial void LogEstablishingWebSocketProxy(
        this ILogger logger,
        Uri clientWebSocketUri,
        Uri targetWebSocketUri
    );

    [LoggerMessage(
        EventId = 31,
        Message = "Failed to establish WebSocket proxy: {Message}",
        Level = LogLevel.Error
    )]
    internal static partial void LogFailedToEstablishWebSocketProxy(
        this ILogger logger,
        string message
    );

    [LoggerMessage(
        EventId = 32,
        Message = "Failed to close WebSocket {WebSocketUri}. {Message}",
        Level = LogLevel.Warning
    )]
    internal static partial void LogFailedToCloseWebSocket(
        this ILogger logger,
        Uri webSocketUri,
        string message
    );
    #endregion

    #region ViteTagHelper
    [LoggerMessage(
        EventId = 40,
        Message = "vite-{Attribute} value missing (check {View})",
        Level = LogLevel.Warning
    )]
    internal static partial void LogViteAttributeMissing(
        this ILogger logger,
        string attribute,
        string view
    );

    [LoggerMessage(
        EventId = 41,
        Message = "'{Key}' was not found in Vite manifest file (check {View})",
        Level = LogLevel.Error
    )]
    internal static partial void LogViteManifestKeyNotFound(
        this ILogger logger,
        string key,
        string view
    );

    [LoggerMessage(
        EventId = 42,
        Message = "The entry '{Entry}' doesn't have CSS chunks",
        Level = LogLevel.Warning
    )]
    internal static partial void LogEntryDoesntHaveCssChunks(this ILogger logger, string entry);
    #endregion
}
