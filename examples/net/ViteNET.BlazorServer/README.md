# ViteNET

This is an example project to show how to use Vite with Blazor Server.

## How to run

The project is already configured to install the npm dependencies and run vite. So, simply run the ASP.NET application by running `dotnet run` or pressing `F5` in Visual Studio.

## Considerations when using Blazor

Avoid running scripts directly from script tags. Blazor performs a pre-rendering process and if your scripts are executed before this process finishes, all events and references will be removed once rendering is complete.

If you need to run a script after the page loads. It's recommended to import the script into Blazor. See [index.razor](Pages/Index.razor).

The Tag Helpers will be in the [_Host.cshtml](Pages/_Host.cshtml) file.

Include the namespaces in the [_Imports.razor](_Imports.razor) file.
