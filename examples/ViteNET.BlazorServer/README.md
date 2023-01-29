# ViteNET

This is an example project to show how to use Vite with Blazor Server.

## How to run

First, install the npm packages:

```PowerShell
npm install
```

Then, run the Vite dev server:

```PowerShell
npm run dev
```

Finally, run the ASP.NET application by executing the following command or by pressing `F5` in Visual Studio:

```PowerShell
dotnet run
```

## Considerations when using Blazor

Avoid running scripts directly from script tags. Blazor performs a pre-rendering process and if your scripts are executed before this process finishes, all events and references will be removed once rendering is complete.

If you need to run a script after the page loads. It's recommended to import the script into Blazor and call the functions you need. See [index.razor](Pages/Index.razor). You will need to generate ES modules before you can use them in Blazor. A quick option is by dynamically importing scripts from your entry points. Vite will generate chunks ready to be used. See [main.ts](Assets/main.ts).
