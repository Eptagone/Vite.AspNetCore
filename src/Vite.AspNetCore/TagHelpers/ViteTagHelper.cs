// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Vite.AspNetCore.Abstractions;
using Vite.AspNetCore.Services;
using Vite.AspNetCore.Utilities;

namespace Vite.AspNetCore.TagHelpers;

/// <summary>
/// This tag helper is used to replace the vite-src and vite-href attributes with the correct file path according to the entry in the "manifest.json" file.
/// </summary>
[HtmlTargetElement("script", Attributes = VITE_SRC_ATTRIBUTE)]
[HtmlTargetElement("link", Attributes = VITE_HREF_ATTRIBUTE)]
[EditorBrowsable(EditorBrowsableState.Never)]
public class ViteTagHelper : TagHelper
{
	private static readonly Regex ScriptRegex = new(@"\.(js|ts|jsx|tsx|cjs|cts|mjs|mts)$", RegexOptions.Compiled);

	private const string VITE_HREF_ATTRIBUTE = "vite-href";
	private const string VITE_SRC_ATTRIBUTE = "vite-src";

	private readonly ILogger<ViteTagHelper> _logger;
	private readonly IViteManifest _manifest;
	private readonly IUrlHelperFactory _urlHelperFactory;
	private readonly ViteStatusService _status;

	/// <summary>
	/// Initialize a new instance of <see cref="ViteTagHelper"/>
	/// </summary>
	/// <param name="logger">The logger.</param>
	/// <param name="manifest">The manifest service.</param>
	public ViteTagHelper(ILogger<ViteTagHelper> logger, IViteManifest manifest, IUrlHelperFactory urlHelperFactory, ViteStatusService viteStatusService)
	{
		this._logger = logger;
		this._manifest = manifest;
		this._urlHelperFactory = urlHelperFactory;
		this._status = viteStatusService;
	}

	/// <summary>
	/// The entry name in the manifest file.
	/// The manifest can only be accessed after building the assets with 'npm run build'.
	/// </summary>
	[HtmlAttributeName(VITE_SRC_ATTRIBUTE)]
	public string? ViteSrc { get; set; }

	/// <summary>
	/// The entry name in the manifest file.
	/// The manifest can only be accessed after building the assets with 'npm run build'.
	/// </summary>
	[HtmlAttributeName(VITE_HREF_ATTRIBUTE)]
	public string? ViteHref { get; set; }

	/// <inheritdoc />
	[ViewContext]
	[HtmlAttributeNotBound]
	public ViewContext ViewContext { get; set; } = default!;

	// Set the Order property to int.MinValue to ensure this tag helper is executed before any other tag helpers with a higher Order value
	public override int Order => int.MinValue;

	/// <inheritdoc />
	public override void Process(TagHelperContext context, TagHelperOutput output)
	{
		// because some people might shout their tag names like SCRIPT and LINK!
		var tagName = output.TagName.ToLowerInvariant();

		var (attribute, value) = tagName.ToLowerInvariant() switch
		{
			"script" => (attribute: "src", value: this.ViteSrc),
			"link" => (attribute: "href", value: this.ViteHref),
			_ => throw new NotImplementedException("This case should never happen")
		};

		// Remove the vite attribute from the output
		output.Attributes.RemoveAll($"vite-{attribute}");

		// If the value is empty or null, we don't need to do anything
		if (string.IsNullOrWhiteSpace(value))
		{
			this._logger.LogWarning("vite-{Attribute} value missing (check {View})",
				attribute,
				this.ViewContext.View.Path);
			return;
		}

		var urlHelper = this._urlHelperFactory.GetUrlHelper(this.ViewContext);

		string file;

		// If the Vite development server is enabled, don't load the files from the manifest.
		if (ViteStatusService.IsMiddlewareRegistered)
		{
			// If the tagName is a link and the file is a script, destroy the element.
			if (tagName == "link" && ScriptRegex.IsMatch(value))
			{
				output.SuppressOutput();
				return;
			}

			// If the Vite script was not inserted, it will be prepended to the current element tag.
			if (ViteStatusService.IsMiddlewareRegistered && !this._status.IsDevScriptInserted)
			{
				var viteClientPath = urlHelper.Content("~/@vite/client");
				// Add the script tag to the output
				output.PreElement.AppendHtml($"<script type=\"module\" src=\"{viteClientPath}\"></script>");
				// Set the flag to true to avoid adding the script tag multiple times
				this._status.IsDevScriptInserted = true;
			}

			file = urlHelper.Content(value);
		}
		else
		{
			// Removes the leading '~/' from the value. This is needed because the manifest file doesn't contain the leading '~/' or '/'.
			value = value.TrimStart('~', '/');
            
            // Ensure consistent path separators
            value = PathUtils.PathCombine(value);

			// Get the entry chunk from the 'manifest.json' file.
			var entry = this._manifest[value];

			// If the entry is not found, log an error and return
			if (entry == null)
			{
				this._logger.LogError("\"{Key}\" was not found in Vite manifest file (check {View})",
					value,
					this.ViewContext.View.Path);
				output.SuppressOutput();
				return;
			}

			// If the entry name looks like a script and the tagName is a 'link', render all styles from the entry.
			if (tagName == "link" && ScriptRegex.IsMatch(value))
			{
				// Get the styles from the entry
				var cssFiles = entry.Css;
				// Get the number of styles
				var count = cssFiles?.Count() ?? 0;
				// If the entrypoint doesn't have css files, destroy it.
				if (count == 0)
				{
					this._logger.LogWarning("The entry '{Entry}' doesn't have CSS chunks", value);
					output.SuppressOutput();
					return;
				}

				// Get the file path from the 'manifest.json' file
				file = urlHelper.Content("~/" + cssFiles!.First());

				// TODO: Require revision
				// If the entrypoint has more than one css file, render all styles.
				/*if (count > 1)
				{
					var cssAttr = output.Attributes
						.Where(a => a.Name != "href")
						.Select(a => $"{a.Name}={a.Value}");
					var cssAttrString = string.Join(' ', cssAttr);
					// Get all styles except the first one (the first one is already rendered) and reverse the order.
					var cssExtraFiles = cssFiles!.Skip(1).Reverse();
					// Render all styles
					foreach (var item in cssExtraFiles)
					{
						var href = urlHelper.Content(item);
						// Append the copy next to the current tag.
						output.PostElement.AppendHtml($"<link {cssAttrString} href=\"{href}\" >");
					}
				}*/
			}
			else
			{
				// Get the real file path from the 'manifest.json' file
				file = urlHelper.Content("~/" + entry.File);
			}
		}

		// Update the attributes.
		output.Attributes.SetAttribute(new TagHelperAttribute(
			attribute,
			file,
			HtmlAttributeValueStyle.DoubleQuotes)
		);
	}
}
