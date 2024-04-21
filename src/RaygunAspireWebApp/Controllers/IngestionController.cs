using Microsoft.AspNetCore.Mvc;
using Mindscape.Raygun4Net.AspNetCore;
using System.Text;
using System.Text.Json;

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

          System.IO.File.WriteAllText($"{ErrorsFolderPath}/{uniqueSlug}|{message}.json", requestBody);

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
            .OrderBy(filePath => filePath.CreationTime).ToList();

      if (files.Count > RetentionCount)
      {
        var countToDelete = files.Count - RetentionCount;
        for (var i = 0; i < countToDelete; i++)
        {
          var file = files[i];
          System.IO.File.Delete(file.FullName);
        }
      }
    }
  }
}
