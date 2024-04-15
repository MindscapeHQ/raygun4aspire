using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace RaygunAspireWebApp.Controllers
{
  public class IngestionController : Controller
  {
    public const string ErrorsFolderPath = "/app/raygun/errors";

    public async Task<IActionResult> Entries()
    {
      try
      {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        string requestBody = await reader.ReadToEndAsync();

        var raygunMessage = JsonSerializer.Deserialize<Mindscape.Raygun4Net.RaygunMessage>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new RaygunIdentifierMessageConverter() } });

        if (raygunMessage != null)
        {
          var info = Directory.CreateDirectory(ErrorsFolderPath);

          var message = raygunMessage.Details.Error.Message;
          var uniqueSlug = DateTime.UtcNow.Ticks;

          System.IO.File.WriteAllText($"{ErrorsFolderPath}/{uniqueSlug}|{message}.json", requestBody);
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
      }

      return Accepted();
    }
  }
}
