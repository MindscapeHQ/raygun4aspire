using Mindscape.Raygun4Net.AspNetCore;
using RaygunAspireWebApp.Hubs;
using RaygunAspireWebApp.Ollama;

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

      var connectionString = builder.Configuration.GetConnectionString("Raygun-AIER");
      Console.WriteLine("============ CONNECTION STRING ======================");
      Console.WriteLine(connectionString);
      var section = builder.Configuration.GetSection("ConnectionStrings");
      foreach (var conn in section.GetChildren())
      {
        Console.WriteLine($"{conn.Key}:{conn.Value}");
      }

      builder.Services.AddRaygun((settings) =>
      {
        settings.ApiKey = "LdX7TdiUow6S4UDRS0iPg";
        settings.IsRawDataIgnored = true;
      });

      builder.Services.AddSignalR();
      builder.Services.AddMemoryCache();
      builder.Services.Add(new ServiceDescriptor(typeof(OllamaClient), new OllamaClient()));

      var app = builder.Build();

      var raygunClient = app.Services.GetService<RaygunClient>();

      if (raygunClient != null)
      {
        raygunClient.SendingMessage += (sender, eventArgs) =>
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
