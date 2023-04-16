// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace ViteNET.BlazorServer.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
	public string? RequestId { get; set; }

	public bool ShowRequestId => !string.IsNullOrEmpty(this.RequestId);

	private readonly ILogger<ErrorModel> _logger;

	public ErrorModel(ILogger<ErrorModel> logger)
	{
		this._logger = logger;
	}

	public void OnGet()
	{
		this.RequestId = Activity.Current?.Id ?? this.HttpContext.TraceIdentifier;
	}
}