# Vite.AspNetCore

[![NuGet version (Vite.AspNetCore)](https://img.shields.io/nuget/v/Vite.AspNetCore.svg?style=flat-square&color=rgba(189,52,254,1))](https://www.nuget.org/packages/Vite.AspNetCore/)

This library offers integration with [ViteJS](https://vitejs.dev/) to be used in ASP.NET applications. It's made to work mainly with MPA (Multi-Page Application).

The library is compatible with:

- MVC
- Razor Pages
- Blazor Server

## Features

This library has the following simple but very useful features:

- Tag Helpers for script and link tags.
- A service to access the Vite manifest.
- A Middleware to forward the requests to the Vite Development Server.
- An option to automatically start the Vite Development Server for you. ❤️

## Setup

Install the package from NuGet.

```PowerShell
dotnet add package Vite.AspNetCore
```

Add the following lines to your `Program.cs` or `Startup` class.

```CSharp
using Vite.AspNetCore.Extensions;

// ---- Service Configuration ----
// Add Vite services.
builder.Services.AddViteServices();

// ---- App Configuration ----
// Use the Vite Development Server in development environment.
if (app.Environment.IsDevelopment())
{
    // If you use the integrated middleware (see next line) and your app
    // has not added WebSockets earlier in the pipeline, you need to add it
    // to support HMR (hot module reload).
    /* app.UseWebSockets(); */
    // Enable all required features to use the Vite Development Server.
    // Pass true if you want to use the integrated middleware.
    app.UseViteDevelopmentServer(/* false */);
}
```

## Usage

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

- Middleware is enabled:
  - If the link tag is a script (you want to include css from a script entrypoint), the link tag will just disappear. This is because Vite loads the styles automatically by including the script.
  - If the script of the Vite client is not included, it will be added automatically.
- Middleware is disabled:
  - The link and script tags will be rendered using the original paths taken from the manifest. The value of the `vite-href` and `vite-src` attributes will be used as the entrypoint to access the manifest.
  - If the link tag is a script but the rel attribute is not "stylesheet", the script url will be rendered instead the css url.

The rendered HTML when the middleware is enabled will look like this.

```HTML
<!-- This line includes your styles entrypoints -->

<!-- This line includes your "main.ts" and "secondary.ts" entrypoints -->
<script type="module" src="http://localhost:5173/@vite/client"></script>
<script type="module" src="http://localhost:5173/main.ts"></script>
<script type="module" src="http://localhost:5173/secondary.ts"></script>
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

### The Vite Manifest

The Vite Manifest is a JSON file that contains the mapping between the original file names and the hashed names. This is useful to access the files in production environments.

By using the Vite Manifest service, you can access the manifest in your application by injecting the `IViteManifest` service. There's also a `IViteDevServerStatus` service that provides information about the Vite Development Server. See the following example.

```HTML
@inject IViteManifest Manifest
@inject IViteDevServerStatus DevServerStatus

<environment include="Development">
    <!-- Vite development server script -->
    <script type="module" src="@DevServerStatus.ServerUrlWithBasePath/@@vite/client"></script>
    <script type="module" src="@DevServerStatus.ServerUrlWithBasePath/main.ts"></script>
</environment>
<environment include="Production">
    <script type="module" src="~/@Manifest["main.ts"]!.File" asp-append-version="true"></script>
</environment>
```

You can also inject both services in your controllers or services. See the following example.

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

### The Middleware

The [common way](https://vitejs.dev/guide/backend-integration.html) to access **Vite Development Server** assets in your application is by using the following template, specifying the local URL where Vite Server is running.

```HTML
<!-- Entry point for development -->
<environment include="Development">
    <script type="module" src="http://localhost:5173/@@vite/client"></script>
    <script type="module" src="http://localhost:5173/main.js"></script>
</environment>
<!-- Public assets -->
<environment exclude="Development">
    <img src="http://localhost:5173/assets/logo.svg" alt="Vite Logo" />
</environment>
<environment include="Production">
    <img src="~/assets/logo.svg" alt="Vite Logo" />
</environment>
```

Having to set up two ways to access public assets in different environments doesn't look very good. It can also be a problem in some circumstances. But don't worry, this middleware will solve those problems for you.

By using the vite middleware during development, there's no need need to use the development server URL to resolve your public assets. You can use aspnet paths as usual.

```HTML
<!-- Entry point for development -->
<environment include="Development">
    <!-- It's mandatory to use the full url for the Vite client script. -->
    <script type="module" src="http://localhost:5173/@@vite/client"></script>
    <script type="module" src="http://localhost:5173/main.js"></script>
</environment>

<!-- Public assets -->
<img src="~/assets/logo.svg" alt="Vite Logo" />
```

The middleware will proxy all requests to the Vite Development Server. You won't need alternative paths for images or other resources from your public assets. 🙀🙀🙀

To enable the middleware, you need to pass `true` to the `UseViteDevelopmentServer()` method.

> **Note:** The order of the middlewares is important! Put the `app.UseViteDevelopmentServer(true)` call in a position according to your needs. Otherwise, your assets will not be served as expected.

## Configuration

The default configuration should work for most cases. But if you need to change something, you only need to configure the options via the `AddViteServices()` method, using environment variables, user secrets, or your `appsettings.json` file.

Passing the options to the `AddViteServices()` function is as simple as you can see in the following example:

```CSharp
// Program.cs
using Vite.AspNetCore.Extensions;
using Vite.AspNetCore;

// ...
// Add the Vite services.
builder.Services.AddViteServices(options =>
{
    // By default, the manifest file name is ".vite/manifest.json". If your manifest file has a different name, you can change it here.
    options.Manifest = "my-manifest.json",
    // More options...
});
/// ...
```

If you prefer not to hardcode the options, you can use environment variables or user secrets. I suggest using `appsettings.json` and/or `appsettings.Development.json` files to share the default configuration with other developers. This information is not sensitive, so it's safe to share it.

```JSONC
// appsettings.json
{
    "Vite": {
        "Manifest": "my-manifest.json"
    }
}
```

```JSONC
// appsettings.Development.json
{
    "Vite": {
        "Server": {
            // Enable the automatic start of the Vite Development Server. The default value is false.
            "AutoRun": true,
            // The port where the Vite Development Server will be running. The default value is 5173.
            "Port": 5174,
            // If true, the middleware will use HTTPS to connect to the Vite Development Server. The default value is false.
            "Https": false,
        }
    }
}
```

> In the previous example, i used the `appsettings.json` and `appsettings.Development.json` files to keep the configurations for each environment separated. But you can use only one file if you prefer.

### Available Options

There are more options that you can change. All the available options are listed below. ⚙️

| Property               | Description                                                                                                  |
| ---------------------- | ------------------------------------------------------------------------------------------------------------ |
| `Manifest`             | The manifest file name. Default is `.vite/manifest.json` (Vite 5) or `manifest.json` (Vite 4).                  |
| `Base`                 | The subfolder where your assets will be located, including the manifest file, relative to the web root path.    |
| `PackageManager`       | The name of the package manager to use. Default value is `npm`.                                                 |
| `PackageDirectory`     | The directory where the package.json file is located. Default value is the .NET project working directory.      |
| `UseReactRefresh`      | `true` for loading the react-refresh when loading the vite client while developing to enable HMR. |
| `Server:AutoRun`       | Enable or disable the automatic start of the Vite Dev Server. Default value is `false`.                         |
| `Server:Port`          | The port where the Vite Development Server will be running. Default value is `5173`.                            |
| `Server:Host`          | The host where the Vite Dev Server will be running. Default value is `localhost`.                               |
| `Server:TimeOut`       | The timeout in seconds spent waiting for the vite dev server. Default is `5`                                    |
| `Server:Https`         | If true, the middleware will use HTTPS to connect to the Vite Development Server. Default value is `false`.     |
| `Server:ScriptName`    | The script name to run the Vite Development Server. Default value is `dev`.                                     |

> If you are using the `appsettings.json` and/or `appsettings.Development.json` files, all the options must be under the `Vite` property.

## Examples

Do you want to see how to use this library in a real project? Take a look at [these examples](https://github.com/Eptagone/Vite.AspNetCore/tree/main/examples)
