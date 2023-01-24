// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

namespace Vite.AspNetCore.Abstractions
{
	/// <summary>
	/// Represents a Vite manifest file.
	/// </summary>
	public interface IViteManifest
	{
		/// <summary>
		/// Gets the Vite chunk for the specified entry point if it exists.
		/// If Dev Server is enabled, this will always return <see langword="null"/>.
		/// </summary>
		/// <param name="key"></param>
		/// <returns>The chunk if it exists, otherwise <see langword="null"/>.</returns>
		IViteChunk? this[string key] { get; }
	}
}