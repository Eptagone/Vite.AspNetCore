// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ViteNET.MVC.Models;

namespace ViteNET.MVC.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        this._logger = logger;
    }

    public IActionResult Index()
    {
        return this.View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return this.View(
            new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? this.HttpContext.TraceIdentifier
            }
        );
    }

    [HttpGet("/hello")]
    [HttpPost("/hello")]
    public IActionResult Hello()
    {
        string randomString = Guid.NewGuid().ToString();
        return this.Ok($"Hello World! GUID: {randomString}");
    }
}
