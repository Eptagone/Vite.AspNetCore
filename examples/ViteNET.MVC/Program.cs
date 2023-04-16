// Copyright (c) 2023 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Vite.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
// Add the Vite services.
builder.Services.AddViteServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	// Use Vite Dev Server as middleware.
	app.UseViteDevMiddleware();
}
// Configure the HTTP request pipeline.
else
{
	app.UseExceptionHandler("/Home/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
