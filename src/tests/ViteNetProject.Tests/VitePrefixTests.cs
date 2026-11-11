// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Vite.AspNetCore;
using Vite.AspNetCore.TagHelpers;

namespace ViteNetProject.Tests;

/// <summary>
/// Unit tests for the vite-prefix attribute functionality.
/// </summary>
[Collection("Sequential")]
public class VitePrefixTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    /// <summary>
    /// Initializes a new instance of <see cref="VitePrefixTests"/>.
    /// </summary>
    /// <param name="factory"></param>
    public VitePrefixTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task TagHelper_WithoutPrefix_UsesDefaultBehavior()
    {
        // Arrange
        IWebHostEnvironment hostEnv =
            this.factory.Services.GetRequiredService<IWebHostEnvironment>();
        string manifestPath = Path.Combine(hostEnv.WebRootPath, ".vite", "manifest.json");
        string backup = await File.ReadAllTextAsync(manifestPath);

        try
        {
            string testManifest = """
                {
                  "src/main.ts": {
                    "file": "js/main.abc123.js",
                    "isEntry": true,
                    "src": "src/main.ts"
                  }
                }
                """;
            await File.WriteAllTextAsync(manifestPath, testManifest);
            // Wait for manifest to refresh
            await Task.Delay(100);

            var tagHelper = this.CreateTagHelper();
            var context = this.CreateTagHelperContext("script");
            var output = this.CreateTagHelperOutput("script", "src");

            tagHelper.ViteSrc = "~/src/main.ts";

            // Act
            tagHelper.Process(context, output);

            // Assert
            Assert.Equal("/js/main.abc123.js", output.Attributes["src"]?.Value);
            Assert.Null(output.Attributes["vite-src"]);
            Assert.Null(output.Attributes["vite-prefix"]);
        }
        finally
        {
            await File.WriteAllTextAsync(manifestPath, backup);
        }
    }

    [Fact]
    public async Task TagHelper_WithRelativePrefix_PrependsPrefix()
    {
        // Arrange
        IWebHostEnvironment hostEnv =
            this.factory.Services.GetRequiredService<IWebHostEnvironment>();
        string manifestPath = Path.Combine(hostEnv.WebRootPath, ".vite", "manifest.json");
        string backup = await File.ReadAllTextAsync(manifestPath);

        try
        {
            string testManifest = """
                {
                  "src/main.ts": {
                    "file": "js/main.abc123.js",
                    "isEntry": true,
                    "src": "src/main.ts"
                  }
                }
                """;
            await File.WriteAllTextAsync(manifestPath, testManifest);
            // Wait for manifest to refresh
            await Task.Delay(100);

            var tagHelper = this.CreateTagHelper();
            var context = this.CreateTagHelperContext("script");
            var output = this.CreateTagHelperOutput("script", "src");

            tagHelper.ViteSrc = "~/src/main.ts";
            tagHelper.VitePrefix = "..";

            // Act
            tagHelper.Process(context, output);

            // Assert
            Assert.Equal("../js/main.abc123.js", output.Attributes["src"]?.Value);
            Assert.Null(output.Attributes["vite-src"]);
            Assert.Null(output.Attributes["vite-prefix"]);
        }
        finally
        {
            await File.WriteAllTextAsync(manifestPath, backup);
        }
    }

    [Fact]
    public async Task TagHelper_WithAbsolutePrefix_PrependsPrefix()
    {
        // Arrange
        IWebHostEnvironment hostEnv =
            this.factory.Services.GetRequiredService<IWebHostEnvironment>();
        string manifestPath = Path.Combine(hostEnv.WebRootPath, ".vite", "manifest.json");
        string backup = await File.ReadAllTextAsync(manifestPath);

        try
        {
            string testManifest = """
                {
                  "src/main.ts": {
                    "file": "js/main.abc123.js",
                    "isEntry": true,
                    "src": "src/main.ts"
                  }
                }
                """;
            await File.WriteAllTextAsync(manifestPath, testManifest);
            // Wait for manifest to refresh
            await Task.Delay(100);

            var tagHelper = this.CreateTagHelper();
            var context = this.CreateTagHelperContext("script");
            var output = this.CreateTagHelperOutput("script", "src");

            tagHelper.ViteSrc = "~/src/main.ts";
            tagHelper.VitePrefix = "/static/";

            // Act
            tagHelper.Process(context, output);

            // Assert
            Assert.Equal("/static/js/main.abc123.js", output.Attributes["src"]?.Value);
            Assert.Null(output.Attributes["vite-src"]);
            Assert.Null(output.Attributes["vite-prefix"]);
        }
        finally
        {
            await File.WriteAllTextAsync(manifestPath, backup);
        }
    }


    [Fact]
    public async Task TagHelper_WithTildePrefix_UsesUrlHelperContent()
    {
        // Arrange
        IWebHostEnvironment hostEnv =
            this.factory.Services.GetRequiredService<IWebHostEnvironment>();
        string manifestPath = Path.Combine(hostEnv.WebRootPath, ".vite", "manifest.json");
        string backup = await File.ReadAllTextAsync(manifestPath);

        try
        {
            string testManifest = """
                {
                  "src/main.ts": {
                    "file": "js/main.abc123.js",
                    "isEntry": true,
                    "src": "src/main.ts"
                  }
                }
                """;
            await File.WriteAllTextAsync(manifestPath, testManifest);
            // Wait for manifest to refresh
            await Task.Delay(100);

            var tagHelper = this.CreateTagHelper();
            var context = this.CreateTagHelperContext("script");
            var output = this.CreateTagHelperOutput("script", "src");

            tagHelper.ViteSrc = "~/src/main.ts";
            tagHelper.VitePrefix = "~/prefix/";

            // Act
            tagHelper.Process(context, output);

            // Assert
            // When prefix starts with ~, UrlHelper.Content is used
            Assert.Equal("/prefix/js/main.abc123.js", output.Attributes["src"]?.Value);
            Assert.Null(output.Attributes["vite-src"]);
            Assert.Null(output.Attributes["vite-prefix"]);
        }
        finally
        {
            await File.WriteAllTextAsync(manifestPath, backup);
        }
    }

    [Fact]
    public async Task TagHelper_WithPrefixOnCssLink_AppliesPrefixToCssFiles()
    {
        // Arrange
        IWebHostEnvironment hostEnv =
            this.factory.Services.GetRequiredService<IWebHostEnvironment>();
        string manifestPath = Path.Combine(hostEnv.WebRootPath, ".vite", "manifest.json");
        string backup = await File.ReadAllTextAsync(manifestPath);

        try
        {
            string testManifest = """
                {
                  "src/main.ts": {
                    "file": "js/main.abc123.js",
                    "isEntry": true,
                    "src": "src/main.ts",
                    "css": ["css/main.abc123.css"]
                  }
                }
                """;
            await File.WriteAllTextAsync(manifestPath, testManifest);
            // Wait for manifest to refresh
            await Task.Delay(100);

            var tagHelper = this.CreateTagHelper();
            var context = this.CreateTagHelperContext("link");
            var output = this.CreateTagHelperOutput("link", "href");
            output.Attributes.SetAttribute("rel", "stylesheet");

            tagHelper.ViteHref = "~/src/main.ts";
            tagHelper.VitePrefix = "../";

            // Act
            tagHelper.Process(context, output);

            // Assert
            Assert.Equal("../css/main.abc123.css", output.Attributes["href"]?.Value);
            Assert.Null(output.Attributes["vite-href"]);
            Assert.Null(output.Attributes["vite-prefix"]);
        }
        finally
        {
            await File.WriteAllTextAsync(manifestPath, backup);
        }
    }

    [Fact]
    public async Task TagHelper_WithPrefixWithoutTrailingSlash_AddsSlash()
    {
        // Arrange
        IWebHostEnvironment hostEnv =
            this.factory.Services.GetRequiredService<IWebHostEnvironment>();
        string manifestPath = Path.Combine(hostEnv.WebRootPath, ".vite", "manifest.json");
        string backup = await File.ReadAllTextAsync(manifestPath);

        try
        {
            string testManifest = """
                {
                  "src/main.ts": {
                    "file": "js/main.abc123.js",
                    "isEntry": true,
                    "src": "src/main.ts"
                  }
                }
                """;
            await File.WriteAllTextAsync(manifestPath, testManifest);
            // Wait for manifest to refresh
            await Task.Delay(100);

            var tagHelper = this.CreateTagHelper();
            var context = this.CreateTagHelperContext("script");
            var output = this.CreateTagHelperOutput("script", "src");

            tagHelper.ViteSrc = "~/src/main.ts";
            tagHelper.VitePrefix = "/static"; // No trailing slash

            // Act
            tagHelper.Process(context, output);

            // Assert
            Assert.Equal("/static/js/main.abc123.js", output.Attributes["src"]?.Value);
        }
        finally
        {
            await File.WriteAllTextAsync(manifestPath, backup);
        }
    }

    private ViteTagHelper CreateTagHelper()
    {
        // Create a scope to resolve scoped services
        var scope = this.factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;

        var logger = scopedServices.GetRequiredService<ILogger<ViteTagHelper>>();
        var manifest = scopedServices.GetRequiredService<IViteManifest>();
        var devServerStatus = scopedServices.GetRequiredService<IViteDevServerStatus>();
        var helperService = scopedServices.GetRequiredService<ViteTagHelperMonitor>();
        var viteOptions = scopedServices.GetRequiredService<IOptions<ViteOptions>>();
        var urlHelperFactory = scopedServices.GetRequiredService<IUrlHelperFactory>();

        var tagHelper = new ViteTagHelper(
            logger,
            manifest,
            devServerStatus,
            helperService,
            viteOptions,
            urlHelperFactory
        );

        // Create a ViewContext for the tag helper
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = scopedServices;
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor()
        );
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        var viewContext = new ViewContext(
            actionContext,
            Mock.Of<IView>(),
            viewData,
            tempData,
            TextWriter.Null,
            new HtmlHelperOptions()
        );

        tagHelper.ViewContext = viewContext;

        return tagHelper;
    }

    private TagHelperContext CreateTagHelperContext(string tagName)
    {
        return new TagHelperContext(
            tagName,
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString()
        );
    }

    private TagHelperOutput CreateTagHelperOutput(string tagName, string attributeName)
    {
        var attributes = new TagHelperAttributeList();
        return new TagHelperOutput(
            tagName,
            attributes,
            (useCachedResult, encoder) =>
                Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())
        );
    }
}