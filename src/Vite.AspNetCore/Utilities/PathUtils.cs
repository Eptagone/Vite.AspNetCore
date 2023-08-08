namespace Vite.AspNetCore.Utilities;

public static class PathUtils
{
    private static readonly char[] PathSeparators = { '/', '\\' };
    
    /// <summary>
    /// Combines multiple paths into a single path with normalized separators.
    /// This method is similar to <see cref="Path.Combine(string,string)"/>, which doesn't normalize the separators.
    /// </summary>
    /// <param name="paths">The paths to be combined.</param>
    /// <returns>The combined path with normalized separators.</returns>
    public static string PathCombine(params string[] paths)
    {
        // Split the paths into segments
        var segments = paths.SelectMany(s => s.Split(PathSeparators));
        
        // Join the segments using the platform-specific path separator
        return Path.Combine(segments.ToArray());
    }
}
