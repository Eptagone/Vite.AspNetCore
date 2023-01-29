// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Vite.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Vite service to the builder
builder.Services.AddViteManifest();

// Add services to the container.
builder.Services.AddRazorPages();

if (builder.Environment.IsDevelopment())
{
	// Add the Vite Middleware service.
	builder.Services.AddViteDevMiddleware();
}
// Add the Vite Manifest Service.
builder.Services.AddViteManifest();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
	// Use Vite Dev Server as middleware.
	app.UseViteDevMiddleware();
}
// Configure the HTTP request pipeline.
else
{
	app.UseExceptionHandler("/Error");
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
