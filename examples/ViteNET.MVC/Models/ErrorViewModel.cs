// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

namespace ViteNET.MVC.Models;

public class ErrorViewModel
{
	public string? RequestId { get; set; }

	public bool ShowRequestId => !string.IsNullOrEmpty(this.RequestId);
}