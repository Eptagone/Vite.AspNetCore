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

    [Fact]
    public async Task ManifestIsRefreshedWhenUpdated()
    {
        IWebHostEnvironment hostEnv =
            this._factory.Services.GetRequiredService<IWebHostEnvironment>();
        string manifestPath = Path.Combine(hostEnv.WebRootPath, ".vite", "manifest.json");
        string backup = File.ReadAllText(manifestPath);

        // Setup manifest to consistant starting state
        string testManifest = """
            {
              "src/main.ts": {
                "file": "js/main.111111.js",
                "isEntry": true,
                "src": "src/main.ts"
              }
            }
            """;
        File.WriteAllText(manifestPath, testManifest);
        var manifest = this._factory.Services.GetRequiredService<IViteManifest>();
        IViteChunk chunk = manifest["src/main.ts"]!;
        string initFile = chunk.File;

        // Try updating the manifest
        testManifest = """
            {
              "src/main.ts": {
                "file": "js/main.222222.js",
                "isEntry": true,
                "src": "src/main.ts"
              }
            }
            """;
        File.WriteAllText(manifestPath, testManifest);

        // The refresh is asynchronous, so give it a second to refresh
        await Task.Delay(50);

        chunk = manifest["src/main.ts"]!;
        string fileAfterChange = chunk.File;

        // Restore the file
        File.WriteAllText(manifestPath, backup);

        // Check that we got back what we expect
        Assert.Equal("js/main.111111.js", initFile);
        Assert.Equal("js/main.222222.js", fileAfterChange);
    }
}
