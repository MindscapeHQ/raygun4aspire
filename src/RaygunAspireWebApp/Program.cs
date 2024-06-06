using Mindscape.Raygun4Net.AspNetCore;
using OllamaSharp;
using RaygunAspireWebApp.Hubs;

namespace RaygunAspireWebApp
{
  public class Program
  {
    public static void Main(string[] args)
    {
      var builder = WebApplication.CreateBuilder(args);

      // Add services to the container.
      builder.Services.AddSession();
      builder.Services.AddControllersWithViews();

      builder.Services.AddRaygun((settings) =>
      {
        settings.ApiKey = "";
        settings.IsRawDataIgnored = true;
      });

      builder.Services.AddSignalR();
      builder.Services.AddMemoryCache();

      var connectionString = builder.Configuration.GetConnectionString("Ollama");
      if (!string.IsNullOrWhiteSpace(connectionString))
      {
        builder.Services.Add(new ServiceDescriptor(typeof(IOllamaApiClient), new OllamaApiClient(connectionString, Constants.AiModel)));
      }

      var app = builder.Build();

      var raygunClient = app.Services.GetService<RaygunClient>();

      if (raygunClient != null)
      {
        raygunClient.SendingMessage += (_, eventArgs) =>
        {
          if (eventArgs?.Message?.Details != null)
          {
            eventArgs.Message.Details.MachineName = null;
          }

          if (eventArgs?.Message?.Details?.Request != null)
          {
            eventArgs.Message.Details.Request.IPAddress = null;
          }
        };
      }

      app.UseRaygun();

      // Configure the HTTP request pipeline.
      if (!app.Environment.IsDevelopment())
      {
        app.UseExceptionHandler("/Home/Error");
      }

      app.UseSession();

      app.UseStaticFiles();

      app.UseRouting();

      app.UseAuthorization();

      app.MapControllerRoute(
          name: "default",
          pattern: "{controller=Home}/{action=Index}/{id?}");

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapHub<AierHub>("/aierHub");
      });

      app.Run();
    }
  }
}
