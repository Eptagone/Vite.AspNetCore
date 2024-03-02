// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

namespace Vite.AspNetCore;

/// <summary>
/// Represents a Vite manifest file.
/// </summary>
public interface IViteManifest : IEnumerable<IViteChunk>
{
    /// <summary>
    /// Gets the Vite chunk for the specified entry point if it exists.
    /// If Dev Server is enabled, this will always return <see langword="null"/>.
    /// </summary>
    /// <param name="key"></param>
    /// <returns>The chunk if it exists, otherwise <see langword="null"/>.</returns>
    IViteChunk? this[string key] { get; }

    /// <summary>
    /// Gets an enumerable collection that contains the chunk keys in the manifest.
    /// </summary>
    /// <returns>An enumerable collection that contains the chunk keys in the manifest.</returns>
    IEnumerable<string> Keys { get; }

    /// <summary>
    /// Determines whether the manifest contains a chunk with the specified key entry.
    /// </summary>
    /// <param name="key">The eky entry to locate.</param>
    /// <returns>true if the manifest contains a chunk with the specified key entry; otherwise, false.</returns>
    bool ContainsKey(string key);
}
