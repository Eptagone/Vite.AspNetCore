// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Vite.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
// Add the Vite services.
builder.Services.AddViteServices(options =>
{
	options.Server.AutoRun = true;
	options.Server.Https = true;
	options.Server.UseFullDevUrl = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

if (app.Environment.IsDevelopment())
{
	// Use Vite Dev Server as middleware.
	app.UseViteDevMiddleware();
}

app.Run();
