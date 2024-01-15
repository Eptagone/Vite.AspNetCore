// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Vite.AspNetCore.Services;

namespace Vite.AspNetCore.TagHelpers;

/// <summary>
/// This tag helper is used to replace the vite-src and vite-href attributes with the correct file path according to the entry in the "manifest.json" file.
/// </summary>
/// <param name="logger">The logger.</param>
/// <param name="helperService">The ViteTagHelperService.</param>
/// <param name="manifest">The manifest service.</param>
/// <param name="devServerStatus">The Vite development server status.</param>
/// <param name="urlHelperFactory">Url helper factory to build the file path.</param>
[HtmlTargetElement("script", Attributes = VITE_SRC_ATTRIBUTE)]
[HtmlTargetElement("link", Attributes = VITE_HREF_ATTRIBUTE)]
[EditorBrowsable(EditorBrowsableState.Never)]
public class ViteTagHelper(ILogger<ViteTagHelper> logger, IViteManifest manifest, IViteDevServerStatus devServerStatus, ViteDevScriptMonitor helperService, IOptions<ViteOptions> viteOptions, IUrlHelperFactory urlHelperFactory) : TagHelper
{
	private static readonly Regex ScriptRegex = new(@"\.(js|ts|jsx|tsx|cjs|cts|mjs|mts)$", RegexOptions.Compiled);

	private const string VITE_HREF_ATTRIBUTE = "vite-href";
	private const string VITE_SRC_ATTRIBUTE = "vite-src";
	private const string LINK_AS_ATTRIBUTE = "stylesheet";
	private const string LINK_AS_STYLE = "style";
	private const string LINK_REL_ATTRIBUTE = "rel";
	private const string LINK_REL_STYLESHEET = "stylesheet";

	private readonly ILogger<ViteTagHelper> logger = logger;
	private readonly ViteDevScriptMonitor helperService = helperService;
	private readonly IViteManifest manifest = manifest;
	private readonly IViteDevServerStatus devServerStatus = devServerStatus;
	private readonly IUrlHelperFactory urlHelperFactory = urlHelperFactory;
	private readonly string? basePath = viteOptions.Value.Base?.Trim('/');

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

	/// <summary>
	/// The rel attribute for the link tag.
	/// </summary>
	[HtmlAttributeName(LINK_REL_ATTRIBUTE)]
	public string? Rel { get; set; }

	/// <summary>
	/// The as attribute for the link tag.
	/// </summary>
	[HtmlAttributeName(LINK_AS_ATTRIBUTE)]
	public string? As { get; set; }

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
			this.logger.LogWarning("vite-{Attribute} value missing (check {View})",
				attribute,
				this.ViewContext.View.Path);
			return;
		}

		// Removes the leading '~/' from the value. This is needed because the manifest file doesn't contain the leading '~/' or '/'.
		value = value.TrimStart('~', '/');
		// If the base path is not null, remove it from the value.
		if (!string.IsNullOrEmpty(this.basePath) && value.StartsWith(this.basePath))
		{
			value = value[this.basePath.Length..].TrimStart('/');
		}

		var urlHelper = this.urlHelperFactory.GetUrlHelper(this.ViewContext);
		string file;

		// If the Vite development server is enabled, don't load the files from the manifest.
		if (this.devServerStatus.IsEnabled)
		{
			// If the tagName is a link and the file is a script, destroy the element.
			if (tagName == "link" && ScriptRegex.IsMatch(value))
			{
				output.SuppressOutput();
				return;
			}

			// If the Vite script was not inserted, it will be prepended to the current element tag.
			if (!this.helperService.IsDevScriptInjected)
			{
				// Build the uri to the vite client script.
				var viteClientUrl = this.devServerStatus.ServerUrlWithBasePath + "/@vite/client";
				// Add the script tag to the output
				output.PreElement.AppendHtml($"<script type=\"module\" src=\"{viteClientUrl}\"></script>");
				// Set the flag to true to avoid adding the script tag multiple times
				this.helperService.IsDevScriptInjected = true;
			}
			// Build the url to the file path.
			file = $"{this.devServerStatus.ServerUrlWithBasePath}/{value}";
		}
		else
		{
			// Get the entry chunk from the 'manifest.json' file.
			var entry = this.manifest[value];

			// If the entry is not found, log an error and return
			if (entry == null)
			{
				this.logger.LogError("\"{Key}\" was not found in Vite manifest file (check {View})", value, this.ViewContext.View.Path);
				output.SuppressOutput();
				return;
			}

			// If the entry name looks like a script and the tagName is a 'link' of kind 'stylesheet', render the css file.
			if (tagName == "link" && (this.Rel == LINK_REL_STYLESHEET || this.As == LINK_AS_STYLE) && ScriptRegex.IsMatch(value))
			{
				// Get the styles from the entry
				var cssFiles = entry.Css;
				// Get the number of styles
				var count = cssFiles?.Count() ?? 0;
				// If the entrypoint doesn't have css files, destroy it.
				if (count == 0)
				{
					this.logger.LogWarning("The entry '{Entry}' doesn't have CSS chunks", value);
					output.SuppressOutput();
					return;
				}

				// Get the file path from the 'manifest.json' file
				file = urlHelper.Content($"~/{(string.IsNullOrEmpty(this.basePath) ? string.Empty : $"{this.basePath}/")}{cssFiles!.First()}");

				// TODO: Handle multiple css files
			}
			else
			{
				// Get the real file path from the 'manifest.json' file
				file = urlHelper.Content($"~/{(string.IsNullOrEmpty(this.basePath) ? string.Empty : $"{this.basePath}/")}{entry.File}");
			}
		}

		// Forwards the rel attribute to the output.
		if (!string.IsNullOrEmpty(this.Rel))
		{
			output.Attributes.SetAttribute(new TagHelperAttribute(LINK_REL_ATTRIBUTE, this.Rel));
		}
		// Forwards the as attribute to the output.
		if (!string.IsNullOrEmpty(this.As))
		{
			output.Attributes.SetAttribute(new TagHelperAttribute(LINK_AS_ATTRIBUTE, this.As));
		}

		// Update the attributes.
		output.Attributes.SetAttribute(new TagHelperAttribute(
			attribute,
			file,
			HtmlAttributeValueStyle.DoubleQuotes)
		);
	}
}
