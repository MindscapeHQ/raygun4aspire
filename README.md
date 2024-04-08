# Raygun4Aspire

[Raygun](http://raygun.com) provider for .NET Aspire projects. Collects crash reports from .NET code and displays them in a locally running Raygun portal. Optionally can be configured to send crash reports to the [Raygun](http://raygun.com) cloud service from your production environment.

# Installation

## 1. Install the NuGet package

You'll want to add the raygun4aspire NuGet package to both the Aspire orchestration project (AppHost), and any .NET projects within your Aspire app where you want to collect crash reports from. Either use the NuGet package management GUI in the IDE you use, OR use the below dotnet command.

```
dotnet add package Raygun4Aspire
```

## 2. Add Raygun to the orchestration project (AppHost)

In `Program.cs` of the AppHost project, add a Raygun4Aspire `using` statement, then call `AddRaygun` on the builder (after the builder is initialized and before it is used to build and run).

```
using Raygun4Aspire;

// The distributed application builder is created here

builder.AddRaygun();

// The builder is used to build and run the app somewhere down here
```

The steps so far will cause a Raygun resource to be listed in the orchestration app. Clicking on the URL of that resource will open a local Raygun portal in a new tab where you'll later be able to view crash reports captured in your local development environment.

## 3. Instrument your .NET projects

In each of the .NET projects in your Aspire app, integrate Raygun4Aspire (installed in step 1) in `Program.cs`. Add a `using` statement, call `AddRaygun` on the WebApplicationBuilder, followed by calling `UseRaygun` on the created application.

```
using Raygun4Aspire;

// The WebApplicationBuilder is created somewhere here

builder.AddRaygun();

// The builder is used to create the application a little later on

app.UseRaygun();

// Then at the end of the file, the app is commanded to run
```

Those are the minimal steps to get Raygun4Aspire capturing unhandled exceptions that occur during web requests in your local development environment. See further down how to also log exceptions from try/catch blocks.

## 4. Optionally send crash reports in production to the Raygun cloud service

In production appsettings file of each of your .NET projects, add the below RaygunSettings section. Substitute in your application API key that Raygun provides after you create a new Application in Raygun.

```
"RaygunSettings": {
  "ApiKey": "YOUR_APP_API_KEY"
}
```

