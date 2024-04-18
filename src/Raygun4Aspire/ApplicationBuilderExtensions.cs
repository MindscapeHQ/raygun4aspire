using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mindscape.Raygun4Net;

namespace Raygun4Aspire
{
  public static class ApplicationBuilderExtensions
  {
    private const string NoApiKeyWarning = "Raygun API Key is not set, please set an API Key in the RaygunSettings.";

    /// <summary>
    /// Checks to see if you have an API Key and registers the Raygun Middleware. If no API Key is found, a warning will be logged.
    /// If you're running your Aspire project locally without a Raygun API Key, then a placeholder API Key is automatically provided for you.
    /// </summary>
    public static IApplicationBuilder UseRaygun(this IApplicationBuilder app)
    {
      var settings = app.ApplicationServices.GetService<RaygunSettings>();
      
      if (settings?.ApiKey == null)
      {
        var logger = app.ApplicationServices.GetService<ILoggerFactory>()?.CreateLogger<RaygunMiddleware>();

        if (logger != null)
        {
          logger.LogWarning(NoApiKeyWarning);
        }
        else
        {
          Console.WriteLine(NoApiKeyWarning);
        }
      }

      return app.UseMiddleware<RaygunMiddleware>();
    }

    /// <summary>
    /// Registers a RaygunClient singleton into the services collection.
    /// </summary>
    /// <param name="builder">Your WebApplicationBuilder.</param>
    /// <param name="options">An optional lambda that allows you to modify the RaygunSettings.</param>
    /// <returns>The WebApplicationBuilder to allow chaining further methods.</returns>
    public static WebApplicationBuilder AddRaygun(this WebApplicationBuilder builder, Action<RaygunSettings>? options = null)
    {
      // Fetch settings from configuration or use default settings
      var settings = builder.Configuration.GetSection("RaygunSettings").Get<RaygunSettings>() ?? new RaygunSettings();

      if (builder.Environment.IsDevelopment())
      {
        settings.ApiEndpoint = new Uri($"http://localhost:{RaygunAspireWebAppBuilderExtensions.DefaultHostPort}/ingestion/entries");
        settings.ApiKey = "Aspire"; // The Raygun client base code requires this to be set - even though we don't need it in the local Aspire environment
      }

      // Override settings with user-provided settings
      options?.Invoke(settings);

      builder.Services.TryAddSingleton(settings);
      builder.Services.TryAddSingleton(s => new RaygunClient(s.GetService<RaygunSettings>()!, s.GetService<IRaygunUserProvider>()!));
      builder.Services.AddHttpContextAccessor();

      return builder;
    }

    /// <summary>
    /// Registers the default User Provider with the DI container. This will use the IHttpContextAccessor to fetch the current user.
    /// </summary>
    /// <remarks>
    /// This will attempt to check if a user is Authenticated and use the Name/Email from the claims to create a RaygunIdentifierMessage.
    /// If you wish to provide your own implementation of IRaygunUserProvider, you can use the <see cref="AddRaygunUserProvider&lt;T&gt;" /> method.
    /// </remarks>
    public static WebApplicationBuilder AddRaygunUserProvider(this WebApplicationBuilder builder)
    {
      builder.Services.TryAddSingleton<IRaygunUserProvider, DefaultRaygunUserProvider>();

      return builder;
    }

    /// <summary>
    /// Registers a custom User Provider with the DI container. This allows you to provide your own implementation of IRaygunUserProvider.
    /// </summary>
    /// <remarks>
    /// Refer to the <see cref="DefaultRaygunUserProvider" /> for an example of how to implement IRaygunUserProvider.
    /// </remarks>
    public static WebApplicationBuilder AddRaygunUserProvider<T>(this WebApplicationBuilder builder) where T : class, IRaygunUserProvider
    {
      // In case the default or any other user provider is already registered, remove it first
      var existing = builder.Services.FirstOrDefault(x => x.ServiceType == typeof(IRaygunUserProvider));

      if (existing != null)
      {
        builder.Services.Remove(existing);
      }

      // Add the new user provider
      builder.Services.TryAddSingleton<IRaygunUserProvider, T>();

      return builder;
    }
  }
}
