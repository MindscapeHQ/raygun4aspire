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

# Manually sending exceptions

It's best practice to log all exceptions that occur within try/catch blocks (that aren't subsequently thrown to be caught by unhandled exception hooks). To do this with Raygun, inject the RaygunClient into any place where you wish to manually send exceptions from. Then use the SendInBackground method of that Raygun client.

Below is an example of doing this in a razor page:

```
@inject Raygun4Aspire.RaygunClient raygunClient

<!-- some html elements would typically be here -->

@code {
  private void Function()
  {
    try
    {
      // whatever code this function is doing
    }
    catch(Exception ex)
    {
      raygunClient.SendInBackground(ex);
    }
  }
}
```

# Port

When running in the local development environment, crash reports are sent to the locally running Raygun portal via port `24605`.

# Additional configuration options and features

The following options can be configured in an appsettings.json file or in code.

For example, in the appsettings.json file:

```
{
  "RaygunSettings": {
    "ApiKey": "YOUR_APP_API_KEY",
    "ImaginaryOption": true,
    ...
  }
}
```

The equivalent to doing this in C# is done in the Program.cs file of an application where you are logging exceptions from. Ammend the line where you `AddRaygun` to the WebApplicationBuilder:

```
builder.AddRaygun(settings =>
{
  settings.ApiKey = "YOUR_APP_API_KEY";
  settings.ImaginaryOption = true;
  ...
});
```

Examples below are shown in appsettings.json format.

## Exclude errors by HTTP status code

You can exclude errors by their HTTP status code by providing an array of status codes to ignore in the configuration. For example if you wanted to exclude errors that return the [I'm a teapot](http://tools.ietf.org/html/rfc2324) response code, you could use the configuration below.

```
"RaygunSettings": {
  "ApiKey": "YOUR_APP_API_KEY",
  "ExcludedStatusCodes": [418]
}
```

## Remove sensitive request fields

If you have sensitive data in an HTTP request that you wish to prevent being transmitted to Raygun, you can provide lists of possible keys (names) to remove. The available options are:

* IgnoreSensitiveFieldNames
* IgnoreQueryParameterNames
* IgnoreFormFieldNames
* IgnoreHeaderNames
* IgnoreCookieNames
* IgnoreServerVariableNames

These can each be set to an array of keys to ignore. Setting an option as `*` will indicate that all the keys will not be sent to Raygun. Placing `*` before, after or at both ends of a key will perform an ends-with, starts-with or contains operation respectively. For example, `IgnoreFormFieldNames: ["*password*"]` will cause Raygun to ignore all form fields that contain "password" anywhere in the name. These options are not case sensitive.

Note: The `IgnoreSensitiveFieldNames` is a catch all option that will be applied to ALL fields in the `RaygunRequestMessage`.

## Remove sensitive data logged from the raw request payload

By default, crash reports in Raygun will include the entire raw request payload where it has access to the HttpContext. If you want to avoid Raygun capturing the raw request payload entirely, then set the IsRawDataIgnored option to true:

```
"RaygunSettings": {
  "ApiKey": "YOUR_APP_API_KEY",
  "IsRawDataIgnored": true
}
```

If you have any request payloads formatted as key-value pairs (e.g. `key1=value1&key2=value2`), then you can set `UseKeyValuePairRawDataFilter` to true and then any fields listed in the `IgnoreSensitiveFieldNames` option will not be sent to Raygun.

```
"RaygunSettings": {
  "ApiKey": "YOUR_APP_API_KEY",
  "UseKeyValuePairRawDataFilter": true,
  "IgnoreSensitiveFieldNames": ["key1"]
}
```

Similarly, if you have any request payloads formatted as XML, then you can set `UseXmlRawDataFilter` to true and then any element names listed in the `IgnoreSensitiveFieldNames` option will not be sent to Raygun.

```
"RaygunSettings": {
  "ApiKey": "YOUR_APP_API_KEY",
  "UseXmlRawDataFilter": true,
  "IgnoreSensitiveFieldNames": ["Password"]
}
```

TODO link to custom filter example

