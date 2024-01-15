// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Vite.AspNetCore;

namespace ViteNetProject.Tests;

/// <summary>
/// Integration tests for TagHelpers.
/// </summary>
public class ManifestTest : IClassFixture<WebApplicationFactory<Program>>
{
	private readonly WebApplicationFactory<Program> _factory;

	/// <summary>
	/// Initializes a new instance of <see cref="ManifestTest"/>.
	/// </summary>
	/// <param name="factory"></param>
	public ManifestTest(WebApplicationFactory<Program> factory)
	{
		this._factory = factory;
	}

	[Fact]
	public void ReadManifest()
	{
		var manifest = this._factory.Services.GetRequiredService<IViteManifest>();
		// Verify tha the chunk with name "Assets/main.ts" exists.
		Assert.NotNull(manifest["src/main.ts"]);
	}
}
