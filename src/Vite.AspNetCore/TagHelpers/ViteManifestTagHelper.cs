// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using Vite.AspNetCore.Abstractions;

namespace Vite.AspNetCore.TagHelpers;

/// <summary>
/// Use the vite-manifest attribute on link or script tags to
/// find the matching file from the vite-generated "assets.manifest.json" file
/// </summary>
[HtmlTargetElement("script", Attributes = ViteManifestAttribute)]
[HtmlTargetElement("link", Attributes = ViteManifestAttribute)]
public class ViteManifestTagHelper : TagHelper
{
    private readonly IViteManifest _manifest;
    private readonly ILogger<ViteManifestTagHelper> _logger;
    private const string ViteManifestAttribute = "vite-manifest";

    public ViteManifestTagHelper(IViteManifest manifest, ILogger<ViteManifestTagHelper> logger)
    {
        this._manifest = manifest;
        this._logger = logger;
    }

    /// <summary>
    /// The key of the entry in "assets.manifest.json"
    /// The manifest can only be accessed after building the assets with 'npm run build'.
    /// </summary>
    [HtmlAttributeName(ViteManifestAttribute)]
    public string? Key { get; set; }

    [ViewContext] [HtmlAttributeNotBound] public ViewContext ViewContext { get; set; } = default!;

    public override int Order => int.MinValue;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrWhiteSpace(this.Key))
        {
            this._logger.LogWarning("vite-manifest value missing on {View}", ViewContext.View.Path);
            return;
        }

        var file = this._manifest[this.Key]?.File;

        if (string.IsNullOrEmpty(file))
        {
            this._logger.LogWarning("\"{Key}\" was not found in Vite manifest (check {View})", this.Key,
                ViewContext.View.Path);
            return;
        }

        // remove the attribute from the output
        output.Attributes.RemoveAll("vite-manifest");

        var attribute = output.TagName switch
        {
            "script" => "src",
            "link" => "href",
            _ => throw new ArgumentOutOfRangeException(nameof(output.TagName), output.TagName)
        };

        output.Attributes.SetAttribute(new TagHelperAttribute(
            attribute,
            $"~/{file}",
            HtmlAttributeValueStyle.DoubleQuotes)
        );
    }
}