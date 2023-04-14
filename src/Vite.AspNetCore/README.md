# Vite.AspNetCore

This library offers integration with [ViteJS](https://vitejs.dev/) to be used in ASP.NET applications. It doesn't require a SPA and can be used with:

- Blazor Server
- MVC
- Razor Pages

## Features

This library has two simple but useful features:

- A Middleware to forward the requests to the Vite Dev Server
  - The middleware can start the Vite Dev Server for you ❤️.
- A service to access the Vite manifest

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

This can be a problem in some circumstances. Service workers, for example, cannot be properly tested in this way and if you're using preprocessors like SASS, you've probably noticed that your 'url()'s aren't resolved correctly during development. Also, the developer would have to prepare two ways to access the public assets in the different environments. But don't worry, this middleware will solve all of those problems for you.

By using the vite middleware during development, you don't need to pass the dev server URL. You can use aspnet paths as usual.

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

> **Note:** By default, the middleware will start the Vite Dev Server for you. But remember, you need to have your package.json file in your project root folder. Disable this feature by setting the `Vite:Server:AutoRun` property to `false`. You'll need to start the Vite Dev Server manually before running your application.

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

You can also access the Vite manifest by using the tag helpers included in the package. Be sure to add the following in your `_ViewImports.cshtml`

```
@addTagHelper *, Vite.AspNetCore
```

Followed by using the `vite-manifest` attribute on `<script>` and `link` tags.

```HTML
<link rel="stylesheet" vite-manifest="main.css" asp-append-version="true" />
<script type="module" vite-manifest="main.ts" asp-append-version="true"></script>
```

You even has access to a `vite-client` tag, which outputs the necessary client script tag.

```html
<vite-client />
<!-- Is equivalent to -->
<script type="module" src="~/@@vite/client"></script>
```

Enable the service by adding these lines to your `Program.cs` or `Startup` class. 👍

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

You can change the configuration for the middleware by overriding the following properties. ⚙️

| Property                 | Description                                                                                            |
| ------------------------ | ------------------------------------------------------------------------------------------------------ |
| `Vite:PackageManager`    | The name of the package manager to use. Default value is `npm`.                                        |
| `Vite:WorkingDirectory`  | The working directory where your package.json file is located. Default value is the content root path. |
| `Vite:Server:AutoRun`    | Enable or disable the automatic start of the Vite Dev Server. Default value is `true`.                 |
| `Vite:Server:TimeOut`    | The timeout in seconds spent waiting for the vite dev server. Default is `5`                           |
| `Vite:Server:Port`       | The port where the Vite Dev Server will be running. Default value is `5173`.                           |
| `Vite:Server:UseHttps`   | If true, the middleware will use HTTPS to connect to the Vite Dev Server. Default value is `false`.    |
| `Vite:Server:ScriptName` | The script name to run the Vite Dev Server. Default value is `dev`.                                    |

See the following example.

```JSON
// appsettings.Development.json
{
    "Vite": {
        "Server": {
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
