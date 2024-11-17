// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

namespace Vite.AspNetCore;

/// <summary>
/// Service used by the ViteTagHelper to track the injection of the vite script and similar.
/// </summary>
public class ViteTagHelperMonitor
{
    /// <summary>
    /// True if the vite script has been injected.
    /// </summary>
    public bool IsDevScriptInjected { get; set; }
}
