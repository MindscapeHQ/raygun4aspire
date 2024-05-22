using Microsoft.AspNetCore.Mvc;
using Mindscape.Raygun4Net;
using Mindscape.Raygun4Net.AspNetCore;
using RaygunAspireWebApp.Models;
using System.Runtime;
using System.Text;
using System.Text.Json;

namespace RaygunAspireWebApp.Controllers
{
  public class ErrorInstanceController : Controller
  {
    private RaygunClient _raygunClient;

    public ErrorInstanceController(RaygunClient raygunClient)
    {
      _raygunClient = raygunClient;
    }

    public IActionResult Details(string id)
    {
      var filePaths = Directory.GetFiles(IngestionController.ErrorsFolderPath).Where(n => Path.GetFileName(n).StartsWith(id)).ToList();

      if (filePaths.Count == 1)
      {
        var fileContents = System.IO.File.ReadAllText(filePaths[0]);

        var raygunMessage = JsonSerializer.Deserialize<RaygunMessage>(fileContents, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new RaygunIdentifierMessageConverter() } });

        var model = new ErrorInstanceViewModel { RaygunMessage = raygunMessage };

        HttpContext.Session.SetString("Model", JsonSerializer.Serialize(model));

        return View(model);
      }

      return View(new ErrorInstanceViewModel());
    }

    public IActionResult TabContent(string tab)
    {
      try
      {
        var modelString = HttpContext.Session.GetString("Model");
        var model = JsonSerializer.Deserialize<ErrorInstanceViewModel>(modelString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new RaygunIdentifierMessageConverter() } });

        Console.WriteLine($"Model: {model}");

        switch (tab)
        {
          case "summary":
            return PartialView("Tabs/_Summary", model);
          case "http":
            return PartialView("Tabs/_Http", model);
          case "rawdata":
            return PartialView("Tabs/_RawData", model);
          default:
            return PartialView("Tabs/_Summary", model);
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
        _raygunClient.SendInBackground(ex);
      }

      return null;
    }

    public IActionResult AIChatWindow()
    {
      return PartialView("AIChatWindow");
    }

    public async Task<IActionResult> AIER()
    {
      //string apiUrl = "http://localhost:24606";
      string modelName = "llama3";
      string question = "How many seats does a Boeing 747 have?";

      var model = new ErrorResolutionModel();

      using (HttpClient client = new HttpClient())
      {
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri("http://host.docker.internal:24606/api/generate"));

        //client.DefaultRequestHeaders.Add("Accept", "*/*");
        //client.DefaultRequestHeaders.Add("User-Agent", "RaygunAIER");

        var requestBody = new
        {
          model = modelName,
          prompt = question
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        requestMessage.Content = content;

        try
        {
          HttpResponseMessage response = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);

          if (response.IsSuccessStatusCode)
          {
            //string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Response from Ollama:");
            //Console.WriteLine(responseBody);
            //model.Response = responseBody;

            using (Stream responseStream = await response.Content.ReadAsStreamAsync())
            {
              using (StreamReader reader = new StreamReader(responseStream))
              {
                while (!reader.EndOfStream)
                {
                  string line = await reader.ReadLineAsync();
                  if (line != null)
                  {
                    Console.WriteLine(line);
                  }
                }
              }
            }
          }
          else
          {
            Console.WriteLine($"Request failed with status code: {response.StatusCode}");
            string errorResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Error response:");
            Console.WriteLine(errorResponse);
            model.Response = errorResponse;
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine("An error occurred:");
          Console.WriteLine(ex.Message);
          model.Response = ex.Message;
        }
      }

      return PartialView("_AIER", model);
    }
  }
}
