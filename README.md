# Vite.AspNetCore

[![NuGet version (Vite.AspNetCore)](https://img.shields.io/nuget/v/Vite.AspNetCore.svg?style=flat-square&color=rgba(189,52,254,1))](https://www.nuget.org/packages/Vite.AspNetCore/)

This library offers integration with [ViteJS](https://vitejs.dev/) to be used in ASP.NET applications. It doesn't require a SPA and can be used with:

- Blazor Server
- MVC
- Razor Pages

## Features

This library has three simple but very useful features:

- A Middleware to forward the requests to the Vite Dev Server
  - The middleware can be start the Vite Dev Server for you ❤️.
- A service to access the Vite manifest.
- Tag Helpers for script and link tags.

## Installation

Install the package from NuGet.

```PowerShell
dotnet add package Vite.AspNetCore
```

Add the following line to your `Program.cs` or `Startup` class.

```CSharp
using Vite.AspNetCore.Extensions;

// ---- Service Configuration ----
// Add Vite services.
builder.Services.AddViteServices();

// ---- App Configuration ----
// Use Middleware in development environment.
if (app.Environment.IsDevelopment())
{
    // Enable the Middleware to use the Vite Development Server.
    app.UseViteDevMiddleware();
}
```

## Usage

### The Vite Middleware

The [common way](https://vitejs.dev/guide/backend-integration.html) to access **Vite Dev Server** assets in your application is by using the following template, specifying the local URL where Vite Server is running.

```HTML
<!-- Entry point for development -->
<environment include="Development">
    <script type="module" src="http://localhost:5173/@@vite/client"></script>
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

Having to set up two ways to access public assets in different environments doesn't look very good. It can also be a problem in some circumstances. Service workers, for example, cannot be properly tested this way and if you are using preprocessors like SASS, you have probably noticed that your 'url()'s are not resolved correctly during development. But don't worry, this middleware will solve all those problems for you.

By using the vite middleware during development, you don't need to pass the development server URL. You can use aspnet paths as usual.

```HTML
<!-- Entry point for development -->
<environment include="Development">
    <script type="module" src="~/@@vite/client"></script>
    <script type="module" src="~/main.js"></script>
</environment>

<!-- Public assets -->
<img src="~/assets/logo.svg" alt="Vite Logo" />
```

The middleware will proxy all requests to the Vite Dev server. You won't need alternative paths for images or other resources from your public assets. 🙀🙀🙀

> **Note:** The middleware can start the Vite Development Server for you. Enable this feature by setting the `Vite:Server:AutoRun` property to `true`. But remember, you need to have your `package.json` file in your project root folder.

### The Vite Manifest

The Vite Manifest is a JSON file that contains the mapping between the original file names and the hashed names. This is useful to access the files in production environments.

By using the Vite Manifest service, you can access the manifest in your application by injecting the `IViteManifest` interface. See the following example.

```HTML
@inject IViteManifest Manifest

<environment include="Development">
    <!-- Vite development server script -->
    <script type="module" src="~/@@vite/client"></script>
    <script type="module" src="~/main.ts"></script>
</environment>
<environment include="Production">
    <script type="module" src="~/@Manifest["main.ts"]!.File" asp-append-version="true"></script>
</environment>
```

You can also inject the manifest service in your controllers or services. See the following example.

```CSharp
public class HomeController : Controller
{
    private readonly IViteManifest _manifest;

    public HomeController(IViteManifest manifest)
    {
        _manifest = manifest;
    }

    public IActionResult Index()
    {
        var mainFile = _manifest["main.ts"]?.File;
        return View();
    }
}
```

### Tag Helpers

Do you want to render your entrypoint scripts and styles in the simplest way possible? You can use the special tag helpers provided by this library. First, add the following line to your `_ViewImports.cshtml` file.

```CSHTML
@addTagHelper *, Vite.AspNetCore
```

Now you can use the `vite-src` and `vite-href` attributes in your scripts and links. See the following example.

```HTML
<!-- This line includes your styles entrypoints -->
<link rel="stylesheet" vite-href="~/main.ts" />

<!-- This line includes your "main.ts" and "secondary.ts" entrypoints -->
<script type="module" vite-src="~/main.ts" asp-append-version="true"></script>
<script type="module" vite-src="~/secondary.ts"></script>
```

This tag helpers will do the following magic:

- If the middleware is enabled:
  - If the link tag is a script (you wnat to include css from a script entrypoint), the link tag will just disappear. This is because Vite loads the styles automatically by including the script.
  - If the script of the vite client is not included, it will be added automatically.
- If the middleware is disabled:
  - The link and script tags will be rendered using the original paths taken from the manifest. The value of the `vite-href` and `vite-src` attributes will be used as the entrypoint to access the manifest.

The rendered HTML when the middleware is enabled will look like this.

```HTML
<!-- This line includes your styles entrypoints -->

<!-- This line includes your "main.ts" and "secondary.ts" entrypoints -->
<script type="module" src="/@vite/client"></script>
<script type="module" src="/main.ts"></script>
<script type="module" src="/secondary.ts"></script>
```

And the rendered HTML when the middleware is disabled will look like this.

```HTML
<!-- This line includes your styles entrypoints -->
<link rel="stylesheet" href="/css/main.css" />

<!-- This line includes your "main.ts" and "secondary.ts" entrypoints -->
<script type="module" src="/js/main.js?v=bosLkDB4bJV3qdsFksYZdubiZvMYj_vuJXBs3vz-nc0"></script>
<script type="module" src="/js/secondary.js"></script>
```

> **Note:** The final paths and filenames depend on how you set it in your `vite.config.ts` file.

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

You can change the configuration for the middleware by overriding the following properties. ⚙️

| Property                 | Description                                                                                         |
| ------------------------ | --------------------------------------------------------------------------------------------------- |
| `Vite:PackageManager`    | The name of the package manager to use. Default value is `npm`.                                     |
| `Vite:Server:AutoRun`    | Enable or disable the automatic start of the Vite Dev Server. Default value is `false`.             |
| `Vite:Server:TimeOut`    | The timeout in seconds spent waiting for the vite dev server. Default is `5`                        |
| `Vite:Server:Port`       | The port where the Vite Dev Server will be running. Default value is `5173`.                        |
| `Vite:Server:UseHttps`   | If true, the middleware will use HTTPS to connect to the Vite Dev Server. Default value is `false`. |
| `Vite:Server:ScriptName` | The script name to run the Vite Dev Server. Default value is `dev`.                                 |

See the following example.

```JSON
// appsettings.Development.json
{
    "Vite": {
        "Server": {
            // Enable the automatic start of the Vite Dev Server. The default value is false.
            "AutoRun": true,
            // The port where the Vite Dev Server will be running. The default value is 5173.
            "Port": 5174,
            // If true, the middleware will use HTTPS to connect to the Vite Dev Server. The default value is false.
            "UseHttps": false,
        }
    }
}
```

## Examples

Do you want to see how to use this library in a real project? Take a look at [these examples](https://github.com/Eptagone/Vite.AspNetCore/tree/main/examples)
