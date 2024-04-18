# Raygun4Aspire

[Raygun](http://raygun.com) provider for .NET Aspire projects. Collects crash reports from .NET code and displays them in a locally running Raygun portal. Optionally can be configured to send crash reports to the [Raygun](http://raygun.com) cloud service from your production environment.

# Installation

## 1. Install the Raygun4Aspire NuGet package

You'll want to add the [Raygun4Aspire NuGet package](https://www.nuget.org/packages/Raygun4Aspire) to both the Aspire orchestration project (AppHost), and any .NET projects within your Aspire app where you want to collect crash reports from. Either use the NuGet package management GUI in the IDE you use, OR use the below dotnet command.

```bash
dotnet add package Raygun4Aspire
```

## 2. Add Raygun to the orchestration project (AppHost)

In `Program.cs` of the AppHost project, add a Raygun4Aspire `using` statement, then call `AddRaygun` on the builder (after the builder is initialized and before it is used to build and run).

```csharp
using Raygun4Aspire;

// The distributed application builder is created here

builder.AddRaygun();

// The builder is used to build and run the app somewhere down here
```

The steps so far will cause a Raygun resource to be listed in the orchestration app. Clicking on the URL of that resource will open a local Raygun portal in a new tab where you'll later be able to view crash reports captured in your local development environment.

## 3. Instrument your .NET projects

In each of the .NET projects in your Aspire app, integrate Raygun4Aspire (installed in step 1) in `Program.cs`. Add a `using` statement, call `AddRaygun` on the WebApplicationBuilder, followed by calling `UseRaygun` on the created application.

```csharp
using Raygun4Aspire;

// The WebApplicationBuilder is created somewhere here

builder.AddRaygun();

// The builder is used to create the application a little later on

app.UseRaygun();

// Then at the end of the file, the app is commanded to run
```

Those are the minimal steps to get Raygun4Aspire capturing unhandled exceptions that occur during web requests in your local development environment. See further down how to also log exceptions from try/catch blocks.

## 4. Optionally send crash reports in production to the Raygun cloud service

In production `appsettings` files of each of your .NET projects, add the below `RaygunSettings` section. Substitute in your application API key that Raygun provides when you create a new Application in Raygun.

```json
"RaygunSettings": {
  "ApiKey": "YOUR_APP_API_KEY"
}
```

# Manually sending exceptions

It's best practice to log all exceptions that occur within try/catch blocks (that aren't subsequently thrown to be caught by unhandled exception hooks). To do this with Raygun, inject the `RaygunClient` into any place where you wish to manually send exceptions from. Then use the `SendInBackground` method of that Raygun client.

Below is an example of doing this in a razor page:

```csharp
@inject Raygun4Aspire.RaygunClient raygunClient

<!-- some html elements would typically be here -->

@code {
  private void Function()
  {
    try
    {
      // whatever code this function is doing
    }
    catch (Exception ex)
    {
      raygunClient.SendInBackground(ex);
    }
  }
}
```

The SendInBackground method also contains optional parameters to send tags, custom data, user details and the HttpContext.

**Tags** is a list of strings that could be used to categorize crash reports.

**Custom data** is a dictionary of key value pairs for logging richer contextual information that can help further understand the cause of an exception specific to your code.

**User details** is a `RaygunIdentifierMessage` object that can be used to capture details about a user impacted by the crash report. Make sure to abide by any privacy policies that your company follows when logging such information. The identifier you set here could instead be an internal database Id, or even just a unique guid to at least gauge how many users are impacted by an exception.

**HttpContext** will cause request and response information to be included in the crash report where applicable.

# Custom user provider

By default Raygun4Aspire ships with a `DefaultRaygunUserProvider` which will attempt to get the user information from the HttpContext.User object. This is Opt-In which can be added as follows:

```csharp
// The WebApplicationBuilder is created somewhere here

// AddRaygun is called here

builder.AddRaygunUserProvider();
```

Alternatively, if you want to provide your own implementation of the `IRaygunUserProvider`, you can do so by creating a class that implements that interface and then registering it via the generic `AddRaygunUserProvider` overload as seen in the example below.

```csharp
public class ExampleUserProvider : IRaygunUserProvider
{
  private readonly IHttpContextAccessor _contextAccessor;
  
  public ExampleUserProvider(IHttpContextAccessor httpContextAccessor)
  {
    _contextAccessor = httpContextAccessor;
  }
  
  public RaygunIdentifierMessage? GetUser()
  {
    var ctx = _contextAccessor.HttpContext;
    
    if (ctx == null)
    {
      return null;
    }

    var identity = ctx.User.Identity as ClaimsIdentity;
    
    if (identity?.IsAuthenticated == true)
    {
      return new RaygunIdentifierMessage(identity.Name)
      {
        IsAnonymous = false
      };
    }
    
    return null;
  }
}
```

This can be registered in the services during configuration like so:

```csharp
// The WebApplicationBuilder is created somewhere here

// AddRaygun is called here

builder.AddRaygunUserProvider<ExampleUserProvider>();
```

# Port

When running in the local development environment, crash reports are sent to the locally running Raygun portal via port `24605`.

# Additional configuration options

The following options can be configured in an appsettings file or in code.

For example, in the `appsettings.json` file:

```json
{
  "RaygunSettings": {
    "ApiKey": "YOUR_APP_API_KEY",
    "ImaginaryOption": true,
    ...
  }
}
```

The equivalent to doing this in C# is done in the `Program.cs` file of an application where you are logging exceptions from. Amend the line where you `AddRaygun` to the WebApplicationBuilder:

```csharp
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

```json
"RaygunSettings": {
  "ApiKey": "YOUR_APP_API_KEY",
  "ExcludedStatusCodes": [418]
}
```

## Remove sensitive request fields

If you have sensitive data in an HTTP request that you wish to prevent being transmitted to Raygun, you can provide lists of possible keys (names) to remove. The available options are:

* IgnoreQueryParameterNames
* IgnoreFormFieldNames
* IgnoreHeaderNames
* IgnoreCookieNames

These can each be set to an array of keys to ignore. Setting an option as `*` will indicate that all the keys will not be sent to Raygun. Placing `*` before, after or at both ends of a key will perform an ends-with, starts-with or contains operation respectively. For example, `IgnoreFormFieldNames: ["*password*"]` will cause Raygun to ignore all form fields that contain "password" anywhere in the name. These options are not case sensitive.

Note: There is also a special `IgnoreSensitiveFieldNames` property which allows you to set common filter lists that apply to all query-parameters, form-fields, headers and cookies. (This setting is also used for filtering the raw request payload as explained further below).

## Remove sensitive data logged from the raw request payload

By default, crash reports in Raygun will include the entire raw request payload where it has access to the HttpContext. If you want to avoid Raygun capturing the raw request payload entirely, then set the IsRawDataIgnored option to true:

```json
"RaygunSettings": {
  "ApiKey": "YOUR_APP_API_KEY",
  "IsRawDataIgnored": true
}
```

If you have any request payloads formatted as key-value pairs (e.g. `key1=value1&key2=value2`), then you can set `UseKeyValuePairRawDataFilter` to true and then any fields listed in the `IgnoreSensitiveFieldNames` option will not be sent to Raygun.

```json
"RaygunSettings": {
  "ApiKey": "YOUR_APP_API_KEY",
  "UseKeyValuePairRawDataFilter": true,
  "IgnoreSensitiveFieldNames": ["key1"]
}
```

Similarly, if you have any request payloads formatted as XML, then you can set `UseXmlRawDataFilter` to true and then any element names listed in the `IgnoreSensitiveFieldNames` option will not be sent to Raygun.

```json
"RaygunSettings": {
  "ApiKey": "YOUR_APP_API_KEY",
  "UseXmlRawDataFilter": true,
  "IgnoreSensitiveFieldNames": ["Password"]
}
```

You can also implement your own `IRaygunDataFilter` to suit your own situations and then register one or more of these custom filters on the RaygunSettings object (a code example for this can be seen after the below RaygunJsonDataFilter implementation example). If the filtering fails, e.g. due to an exception, then null should be returned to indicate this.

Here's an example of a custom raw request data filter for the JSON data structure:

```csharp
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Mindscape.Raygun4Net.Filters;

public class RaygunJsonDataFilter : IRaygunDataFilter
{
  private const string FILTERED_VALUE = "[FILTERED]";

  public bool CanParse(string data)
  {
    if (!string.IsNullOrEmpty(data))
    {
      int index = data.TakeWhile(c => char.IsWhiteSpace(c)).Count();
      if (index < data.Length)
      {
        if (data.ElementAt(index).Equals('{'))
        {
          return true;
        }
      }
    }
    return false;
  }

  public string Filter(string data, IList<string> ignoredKeys)
  {
    try
    {
      JObject jObject = JObject.Parse(data);

      FilterTokensRecursive(jObject.Children(), ignoredKeys);

      return jObject.ToString(Formatting.None, null);
    }
    catch
    {
      return null;
    }
  }

  private void FilterTokensRecursive(IEnumerable<JToken> tokens, IList<string> ignoredKeys)
  {
    foreach (JToken token in tokens)
    {
      if (token is JProperty)
      {
        var property = token as JProperty;

        if (ShouldIgnore(property, ignoredKeys))
        {
          property.Value = FILTERED_VALUE;
        }
        else if (property.Value.Type == JTokenType.Object)
        {
          FilterTokensRecursive(property.Value.Children(), ignoredKeys);
        }
      }
    }
  }

  private bool ShouldIgnore(JProperty property, IList<string> ignoredKeys)
  {
    bool hasValue = property.Value.Type != JTokenType.Null;

    if (property.Value.Type == JTokenType.String)
    {
      hasValue = !string.IsNullOrEmpty(property.Value.ToString());
    }

    return hasValue && !string.IsNullOrEmpty(property.Name) && ignoredKeys.Any(f => f.Equals(property.Name, StringComparison.OrdinalIgnoreCase));
  }
}
```

To get the RaygunClient to use a custom raw data filter, Change the AddRaygun statement to use the overload that takes a lambda where you can modify the RaygunSettings object:

```csharp
builder.AddRaygun((settings) => settings.RawDataFilters.Add(new RaygunJsonDataFilter()));
```

In the case that the filter operation failed (i.e. the filter returned null), then the original raw request data will be included in the Raygun crash report by default. If you instead want a filter failure to cause the entire raw request payload to be excluded from the Raygun crash report, then you can set the `IsRawDataIgnoredWhenFilteringFailed` option to true.

```json
"RaygunSettings": {
  "ApiKey": "YOUR_APP_API_KEY",
  "IsRawDataIgnoredWhenFilteringFailed": true
}
```

## Application version

Raygun4Aspire will attempt to get the version of your application from the running assembly, and include that version number in each crash report. If this is not the version number you would like, you can overwrite it via the ApplicationVersion option:

```json
"RaygunSettings": {
  "ApiKey": "YOUR_APP_API_KEY",
  "ApplicationVersion": "Avacado"
}
```

## Throw exceptions that occur within Raygun4Aspire

This option can help debug issues within Raygun4Aspire itself. It's highly recommended to **not set** this option in your production environment. By default, the Raygun4Aspire client will swallow any exceptions that it encounters. Setting `ThrowOnError` to true will cause said errors to be rethrown instead, which can be useful for troubleshooting Raygun4Aspire.

```json
"RaygunSettings": {
  "ApiKey": "YOUR_APP_API_KEY",
  "ThrowOnError": true
}
```

# Additional features

The following features are set by using the registered RaygunClient singleton. To do this, fetch the RaygunClient singleton from the Services list some time after the builder has been used to build the app.

For example, in `Program.cs` of a .NET app where crash reports will be sent from:

```csharp
// The WebApplicationBuilder is used to build the app somewhere up here

var raygunClient = app.Services.GetService<RaygunClient>();

// Use the raygunClient to set any features here
```

## Modify Raygun crash reports

On the RaygunClient singleton, attach an event handler to the `SendingMessage` event. This event handler will be called just before the RaygunClient sends any crash report to Raygun. The event arguments provide the `RaygunMessage` object that is about to be sent. One use for this event handler is to add or modify any information on the RaygunMessage.

The following example uses the SendingMessage event to set a global tag that will be included on all crash reports.

```csharp
var raygunClient = app.Services.GetService<RaygunClient>();

raygunClient.SendingMessage += (sender, eventArgs) =>
{
  eventArgs.Message.Details.Tags.Add("Web API");
};
```

## Cancel sending certain crash reports to Raygun

On the RaygunClient singleton, attach an event handler to the `SendingMessage` event. This event handler will be called just before the RaygunClient sends any crash report to Raygun. Here you can analyze the crash report that's about to be sent to Raygun and use conditional logic to decide if that crash report is something you know that you don't need logged, so cancel the delivery. Set `eventArgs.Cancel` to true to avoid sending that crash report to Raygun.

Below is a simple example to cancel sending specific exceptions. Be sure to add null checks for properties and bounds checks when looking into arrays.

```csharp
var raygunClient = app.Services.GetService<RaygunClient>();

raygunClient.SendingMessage += (sender, eventArgs) =>
{
  if (eventArgs.Message.Details.Error.Message.Contains("Test exception"))
  {
    eventArgs.Cancel = true;
  }
};
```

## Strip wrapper exceptions

If you have common outer exceptions that wrap a valuable inner exception, you can specify these by using the multi-parameter method:

```csharp
var raygunClient = app.Services.GetService<RaygunClient>();

raygunClient.AddWrapperExceptions(typeof(CustomWrapperException));
```

In this case, if a CustomWrapperException occurs, it will be removed and replaced with the actual InnerException that was the cause. Note that `TargetInvocationException` is already added to the wrapper exception list. This method is useful if you have your own custom wrapper exceptions, or a framework is throwing exceptions using its own wrapper.
