// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Vite.AspNetCore.TagHelpers;

/// <summary>
/// The &lt;vite-client /&gt; generates a script tag pointing to ~/@vite/client
/// </summary>
[HtmlTargetElement(ViteClientTagName, TagStructure = TagStructure.NormalOrSelfClosing)]
public class ViteClientTagHelper : TagHelper
{
    private const string ViteClientTagName = "vite-client";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.Reinitialize("script", TagMode.StartTagAndEndTag);

        // merge attributes
        foreach (var attribute in context.AllAttributes) {
            output.Attributes.Add(attribute);
        }

        output.Attributes.Add("type", "module");
        output.Attributes.Add("src", "~/@vite/client");
    }
}