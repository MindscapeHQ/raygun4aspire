using Microsoft.AspNetCore.Mvc;
using Mindscape.Raygun4Net.AspNetCore;
using System.Text;
using System.Text.Json;
using System.Web;

namespace RaygunAspireWebApp.Controllers
{
  public class IngestionController : Controller
  {
    public const string ErrorsFolderPath = "/app/raygun/errors";

    // If changing this limit, also update it in the README and public-site documentation:
    private const int RetentionCount = 1000;

    private RaygunClient _raygunClient;

    public IngestionController(RaygunClient raygunClient)
    {
      _raygunClient = raygunClient;
    }

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

          var message = raygunMessage.Details?.Error?.Message;
          var uniqueSlug = DateTime.UtcNow.Ticks;

          if (string.IsNullOrWhiteSpace(message))
          {
            message = "Unknown error";
          }

          string noAsterisk = message.Replace("*", "");
          string encodedMessage = HttpUtility.UrlEncode(noAsterisk);
          System.IO.File.WriteAllText($"{ErrorsFolderPath}/{uniqueSlug}-{encodedMessage}.json", requestBody);

          EnforceRetentionAsync();
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
        await _raygunClient.SendInBackground(ex);
      }

      return Accepted();
    }

    private void EnforceRetentionAsync()
    {
      var files = Directory.GetFiles(ErrorsFolderPath)
            .Select(filePath => new FileInfo(filePath))
            .OrderByDescending(filePath => filePath.CreationTime).Skip(RetentionCount).ToList();

      foreach (var file in files)
      {
        System.IO.File.Delete(file.FullName);
      }
    }
  }
}
