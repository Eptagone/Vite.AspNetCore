# Vite.AspNetCore

[![NuGet version (Vite.AspNetCore)](https://img.shields.io/nuget/v/Vite.AspNetCore.svg?style=flat-square&color=rgba(189,52,254,1))](https://www.nuget.org/packages/Vite.AspNetCore/)

This library offers integration with [ViteJS](https://vitejs.dev/) to be used in ASP.NET applications. It's made to work mainly with MPA (Multi-Page Application).

The library is compatible with:

- MVC
- Razor Pages
- Blazor Server

## Features

This library has the following simple but very useful features:

- A Middleware to forward the requests to the Vite Development Server
- A service to access the Vite manifest.
- Tag Helpers for script and link tags.
- Start the Vite Development Server for you ❤️.

## Setup

Install the package from NuGet.

```PowerShell
dotnet add package Vite.AspNetCore
```

Add the following lines to your `Program.cs` or `Startup` class.

```CSharp
using Vite.AspNetCore;

// ---- Service Configuration ----
// Add Vite services.
builder.Services.AddViteServices();

// ---- App Configuration ----
// Use the Vite Development Server when the environment is Development.
if (app.Environment.IsDevelopment())
{
    // WebSockets support is required for HMR (hot module reload).
    // Uncomment the following line if your pipeline doesn't contain it.
    // app.UseWebSockets();
    // Enable all required features to use the Vite Development Server.
    // Pass true if you want to use the integrated middleware.
    app.UseViteDevelopmentServer(/* false */);
}
```

## Usage

### The Vite Middleware

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

Having to set up two ways to access public assets in different environments doesn't look very good. It can also be a problem in some circumstances. Service workers, for example, cannot be properly tested this way and if you are using preprocessors like SASS, you have probably noticed that your 'url()'s are not resolved correctly during development. But don't worry, this middleware will solve all those problems for you.

By using the vite middleware during development, you don't need to pass the development server URL. You can use aspnet paths as usual.

```HTML
<!-- Entry point for development -->
<environment include="Development">
    <!-- It's mandatory to use the full url for the Vite client script. -->
    <script type="module" src="http://localhost:5173/@@vite/client"></script>
    <script type="module" src="~/main.js"></script>
</environment>

<!-- Public assets -->
<img src="~/assets/logo.svg" alt="Vite Logo" />
```

The middleware will proxy all requests to the Vite Development Server. You won't need alternative paths for images or other resources from your public assets. 🙀🙀🙀

To enable the middleware, pass `true` to the `UseViteDevelopmentServer()` method.

> **Note:** The order of the middlewares is important! Put the `UseViteDevelopmentServer(true)` call in a position according to your needs. Otherwise, your assets will not be served as expected.

### The Vite Manifest

The Vite Manifest is a JSON file that contains the mapping between the original file names and the hashed names. This is useful to access the files in production environments.

By using the Vite Manifest service, you can access the manifest in your application by injecting the `IViteManifest` service. See the following example.

```HTML
@inject IViteManifest Manifest

<environment include="Development">
    <!-- Vite development server script -->
    <script type="module" src="http://localhost:5173/@@vite/client"></script>
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

This tag helpers will do the following magic according the state of the Vite Development Server (VDS).

- VDS is enabled:
  - If the link tag is a script (you want to include css from a script entrypoint), the link tag will just disappear. This is because Vite loads the styles automatically by including the script.
  - If the script of the Vite client is not included, it will be added automatically.
- VDS is disabled:
  - The link and script tags will be rendered using the original paths taken from the manifest. The value of the `vite-href` and `vite-src` attributes will be used as the entrypoint to access the manifest.

The rendered HTML when the VDS is enabled will look like this.

```HTML
<!-- This line includes your styles entrypoints -->

<!-- This line includes your "main.ts" and "secondary.ts" entrypoints -->
<script type="module" src="http://localhost:5173/@vite/client"></script>
<script type="module" src="http://localhost:5173/main.ts"></script>
<script type="module" src="http://localhost:5173/secondary.ts"></script>
```

And the rendered HTML when the VDS is disabled will look like this.

```HTML
<!-- This line includes your styles entrypoints -->
<link rel="stylesheet" href="/css/main.css" />

<!-- This line includes your "main.ts" and "secondary.ts" entrypoints -->
<script type="module" src="/js/main.js?v=bosLkDB4bJV3qdsFksYZdubiZvMYj_vuJXBs3vz-nc0"></script>
<script type="module" src="/js/secondary.js"></script>
```

> **Note:** The final paths and filenames depend on how you set it in your `vite.config.ts` file.

## Configuration

The services can be configured by passing options to the `AddViteServices()` function, using environment variables, user secrets, or your `appsettings.json` file.

Passing the options to the `AddViteServices()` function is as simple as you can see in the following example:

```CSharp
// Program.cs
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
            // Pass true, if you are using HTTPS to connect to the Vite Development Server. The default value is false.
            "Https": false,
        }
    }
}
```

> In the previous example, i used the `appsettings.json` and `appsettings.Development.json` files to keep the configurations for each environment separated. But you can use only one file if you prefer.

> Config mechanisms are exclusive to each other and can't be mixed. Service configuration has priority load over Environment Variable configuration.

### Available Options

There are more options that you can change. All the available options are listed below. ⚙️

| Property                  | Description                                                                                                          |
| ------------------------- | -------------------------------------------------------------------------------------------------------------------- |
| `Manifest`                | The manifest file name. Default is `.vite/manifest.json` (Vite 5) or `manifest.json` (Vite 4).                       |
| `Base`                    | The subfolder where your assets will be located, including the manifest file, relative to the web root path.         |
| `Server:Port`             | The port where the Vite Development Server will be running according to your configuration. Default value is `5173`. |
| `Server:Host`             | The host where the Vite Dev Server will be running according to your configuration. Default value is `localhost`.    |
| `Server:TimeOut`          | The timeout in seconds spent waiting for the vite dev server. Default is `5`                                         |
| `Server:Https`            | Set true, if you are using HTTPS to connect to the Vite Development Server. Default value is `false`.                |
| `Server:UseReactRefresh`  | If true, the react-refresh script will be injected before the vite client.                                           |
| `Server:AutoRun`          | Enable or disable the automatic start of the Vite Dev Server. Default value is `false`.                              |
| `Server:PackageManager`   | The name of the package manager to use. Default value is `npm`.                                                      |
| `Server:PackageDirectory` | The directory where the package.json file is located. Default value is the .NET project working directory.           |
| `Server:ScriptName`       | The script name to run the Vite Development Server. Default value is `dev`.                                          |
| `Server:ScriptArgs`       | If specified, the script will be run with the specified arguments. Example: `npm run dev -- [ARGS]`                  |

> If you are using the `appsettings.json` and/or `appsettings.Development.json` files, all the options must be under the `Vite` property.

## Examples

Do you want to see how to use this library in a real project? Take a look at [these examples](https://github.com/Eptagone/Vite.AspNetCore/tree/main/examples)
