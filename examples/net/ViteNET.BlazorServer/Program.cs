using ViteNET.BlazorServer.Data;
using Vite.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();

// Add Vite services
builder.Services.AddViteServices(options =>
{
    options.Server.Https = true;
    options.Server.AutoRun = true;
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

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

if (app.Environment.IsDevelopment())
{
    app.UseWebSockets();
	// Use Vite Dev Server as middleware.
	app.UseViteDevelopmentServer(true);
}

app.Run();
