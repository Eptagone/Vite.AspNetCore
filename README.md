# Vite.AspNetCore

This library offers some integration with the **Vite Dev Server** to be used in ASP.NET applications during development. It doesn't require a SPA and can be used with:

- Razor Pages
- MVC
- Blazor (Server or WASM)

## Features

This library has two simple but useful features:

- A Middleware to forward the requests to the Vite Dev Server
- A service to access the Vite manifest

### The Vite Middleware

The [common way](https://vitejs.dev/guide/backend-integration.html) to access Vite Server assets in your application is by using the following template, specifying the local URL where Vite Server is running.

```HTML
<!-- Entry point for development -->
<environment include="Development">
    <script type="module" src="http://localhost:5173/@vite/client"></script>
    <script type="module" src="http://localhost:5173/main.js"></script>
</environment>
<!-- Public assets -->
<environment include="Development">
    <img src="http://localhost:5173/assets/logo.svg" alt="Vite Logo" />
</environment>
<environment exclude="Development">
    <img src="~/assets/logo.svg" alt="Vite Logo" />
</environment>
```

This can be a problem in some circumstances. Service workers, for example, cannot be properly tested in this way. Also, the developer would have to prepare two ways to access the public assets in the different environments.

By using the middleware during development, you don't need to pass the full local URL. You can use aspnet paths as usual.

```HTML
<!-- Entry point for development -->
<environment include="Development">
    <script type="module" src="~/@vite/client"></script>
    <script type="module" src="~/main.js"></script>
</environment>

<!-- Public assets -->
<img src="~/assets/logo.svg" alt="Vite Logo" />
```

The middleware will proxy all requests to the Vite Dev server. You won't need alternative paths for images or other resources from your public assets.

Enable the middleware by adding these lines to your `Program.cs` or `Startup` class.

```CSharp
using Vite.AspNetCore.Extensions;
// ---- Service Configuration ----
if (builder.Environment.IsDevelopment())
{
    // Add the Vite Middleware service.
    builder.Services.AddViteDevMiddleware();
}
// ...
// ---- App Configuration ----
if (app.Environment.IsDevelopment())
{
    // Use Vite Dev Server as middleware.
    app.UseViteDevMiddleware();
}
```

> **Note:** Don't forget to start the Vite Dev Server before running your application.

### The Vite Manifest

The Vite Manifest is a JSON file that contains the mapping between the original file names and the hashed names. This is useful to access the files in production environments.

By using the Vite Manifest service, you can access the manifest in your application by injecting the `ViteManifest` service. See the following example.

```HTML
@inject ViteManifest Manifest

<environment include="Development">
    <!-- Vite development server script -->
    <script type="module" src="~/@@vite/client"></script>
    <script type="module" src="~/main.ts"></script>
</environment>
<environment include="Production">
    <script type="module" src="~/@Manifest["main.ts"]!.File" asp-append-version="true"></script>
</environment>
```

Enable the service by adding these lines to your `Program.cs` or `Startup` class.

```CSharp
using Vite.AspNetCore.Extensions;
// ---- Service Configuration ----
// Add the Vite Manifest Service.
builder.Services.AddViteManifest();
```

> **Note:** Don't forget to build your assets. Otherwise, the manifest file won't be available.

## Configuration

The middleware and the manifest service can be configured by using environment variables, user secrets or `appsettings.json`.

I suggest using `appsettings.json` and/or `appsettings.Development.json` files. This way, you can share the configuration with other developers. This information is not sensitive, so it's safe to share it.

By default, the manifest name is `manifest.json` and it's expected to be in the web root folder. If your manifest file has a different name, you can change it by setting the `Vite:Manifest` property.

```JSON
// appsettings.json
{
    "Vite": {
        "Manifest": "my-manifest.json"
    }
}
```

By default, the middleware will forward all request to the port `5173` without https. If you need to change the port or the protocol, you can do it by setting the `Vite:Server:Port` and `Vite:Server:UseHttps` properties.

```JSON
// appsettings.Development.json
{
    "Vite": {
        "Server": {
            "Port": 5174,
            "UseHttps": true
        }
    }
}
```
