using Mindscape.Raygun4Net.AspNetCore;

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

      var app = builder.Build();

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

      app.Run();
    }
  }
}
