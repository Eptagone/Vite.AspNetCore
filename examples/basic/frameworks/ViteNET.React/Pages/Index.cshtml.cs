// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ViteNET.RazorPages.Pages;

public class IndexModel : PageModel
{
	private readonly ILogger<IndexModel> _logger;

	public IndexModel(ILogger<IndexModel> logger)
	{
		this._logger = logger;
	}

	public void OnGet()
	{

	}
}