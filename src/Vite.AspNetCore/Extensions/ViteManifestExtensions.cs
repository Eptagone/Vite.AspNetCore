// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

namespace Vite.AspNetCore;

internal static class ViteManifestExtensions
{
    internal static IEnumerable<string> GetRecursiveCssFiles(
        this IViteManifest manifest,
        string chunkName
    ) => GetRecursiveCssFiles(manifest, chunkName, new HashSet<string>());

    private static IEnumerable<string> GetRecursiveCssFiles(
        IViteManifest manifest,
        string chunkName,
        ICollection<string> proccessedChunks
    )
    {
        if (proccessedChunks.Contains(chunkName))
        {
            return [];
        }

        var chunk = manifest[chunkName];
        var cssFiles = new HashSet<string>(chunk?.Css ?? []);
        if (chunk?.Imports?.Any() == true)
        {
            proccessedChunks.Add(chunkName);
            foreach (var import in chunk.Imports)
            {
                var otherCssFiles = GetRecursiveCssFiles(manifest, import, proccessedChunks);
                cssFiles.UnionWith(otherCssFiles);
            }
        }

        return cssFiles.Distinct();
    }
}
