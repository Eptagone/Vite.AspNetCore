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
[HtmlTargetElement("script", Attributes = ViteManifestSrcAttribute)]
[HtmlTargetElement("link", Attributes = ViteManifestHrefAttribute)]
public class ViteManifestTagHelper : TagHelper
{
    private readonly IViteManifest _manifest;
    private readonly ILogger<ViteManifestTagHelper> _logger;
    private const string ViteManifestHrefAttribute = "vite-href";
    private const string ViteManifestSrcAttribute = "vite-src";

    public ViteManifestTagHelper(IViteManifest manifest, ILogger<ViteManifestTagHelper> logger)
    {
        this._manifest = manifest;
        this._logger = logger;
    }

    /// <summary>
    /// The key of the entry in "assets.manifest.json" for script tags
    /// The manifest can only be accessed after building the assets with 'npm run build'.
    /// </summary>
    [HtmlAttributeName(ViteManifestSrcAttribute)]
    public string? Src { get; set; }

    /// <summary>
    /// The key of the entry in "assets.manifest.json" for script tags
    /// The manifest can only be accessed after building the assets with 'npm run build'.
    /// </summary>
    [HtmlAttributeName(ViteManifestHrefAttribute)]
    public string? Href { get; set; }

    /// <summary>
    /// The ViewContext is used to help users find the View with any potential issues
    /// </summary>
    [ViewContext] [HtmlAttributeNotBound] public ViewContext ViewContext { get; set; } = default!;

    public override int Order => int.MinValue;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var tag = output.TagName switch
        {
            "script" => (attribute: "src", value: Src ?? string.Empty),
            "link" => (attribute: "href", value: Href ?? string.Empty),
            _ => throw new ArgumentOutOfRangeException(nameof(output.TagName), output.TagName)
        };
        
        // always attempt to remove the vite attribute from the output
        output.Attributes.RemoveAll($"vite-{tag.attribute}");

        if (string.IsNullOrWhiteSpace(tag.value))
        {
            this._logger.LogWarning("vite-{Attribute} value missing (check {View})",
                tag.attribute,
                this.ViewContext.View.Path);
            return;
        }

        var file = this._manifest[tag.value]?.File;

        if (string.IsNullOrEmpty(file))
        {
            this._logger.LogWarning("\"{Key}\" was not found in Vite manifest file (check {View})",
                tag.value,
                ViewContext.View.Path);
            return;
        }

        output.Attributes.SetAttribute(new TagHelperAttribute(
            tag.attribute,
            $"~/{file}",
            HtmlAttributeValueStyle.DoubleQuotes)
        );
    }
}