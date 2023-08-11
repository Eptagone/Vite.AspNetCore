// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.
// ------------------------------------------------------------------------------
// Created by contributor: Nataniel Pedersen
// Pull Request: https://github.com/Eptagone/Vite.AspNetCore/pull/43

namespace Vite.AspNetCore.Utilities;

internal static class PathUtils
{
	private static readonly char[] PathSeparators = { '/', '\\' };

	/// <summary>
	/// Combines multiple paths into a single path with normalized separators.
	/// This method is similar to <see cref="Path.Combine(string,string)"/>, which doesn't normalize the separators.
	/// </summary>
	/// <param name="paths">The paths to be combined.</param>
	/// <returns>The combined path with normalized separators.</returns>
	internal static string PathCombine(params string[] paths)
	{
		// Split the paths into segments
		var segments = paths.SelectMany(s => s.Split(PathSeparators));

		// Join the segments using the platform-specific path separator
		return Path.Combine(segments.ToArray());
	}
}
