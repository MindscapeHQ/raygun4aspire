using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Mindscape.Raygun4Net;
using Mindscape.Raygun4Net.AspNetCore;
using RaygunAspireWebApp.Hubs;
using RaygunAspireWebApp.Models;
using System.Text;
using System.Text.Json;
using OllamaSharp;

namespace RaygunAspireWebApp.Controllers
{
  public class ErrorInstanceController : Controller
  {
    private RaygunClient _raygunClient;
    private IHubContext<AierHub> _aierHubContext;
    private IMemoryCache _cache;

    private static CancellationTokenSource _cancellationTokenSource;// = new CancellationTokenSource();

    public ErrorInstanceController(RaygunClient raygunClient, IHubContext<AierHub> aierHubContext, IMemoryCache cache)
    {
      _raygunClient = raygunClient;
      _aierHubContext = aierHubContext;
      _cache = cache;
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

    public async Task<IActionResult> AIER()
    {
      _cancellationTokenSource = new CancellationTokenSource();

      try
      {
        await EnsureModel();
      }
      catch (Exception ex)
      {
        // TODO: Raygun crash reporting
        Console.WriteLine("An error occurred:");
        Console.WriteLine(ex.Message);
        return StatusCode(StatusCodes.Status500InternalServerError);
      }

      var modelString = HttpContext.Session.GetString("Model");
      var model = JsonSerializer.Deserialize<ErrorInstanceViewModel>(modelString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new RaygunIdentifierMessageConverter() } });

      string modelName = "llama3";

      var errorModel = model?.RaygunMessage?.Details?.Error;

      var exceptionString = !string.IsNullOrWhiteSpace(errorModel.ClassName) ? $"{errorModel.ClassName}: " : "";
      exceptionString += errorModel.Message;

      string question = "My .NET Aspire application has encountered the following exception. Briefly explain how I should look into fixing it.\r\nThe exception is: " + exceptionString;

      using (HttpClient client = new HttpClient())
      {
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri("http://host.docker.internal:24606/api/generate"));

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
          HttpResponseMessage response = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, _cancellationTokenSource.Token);

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
                  if (_cancellationTokenSource.Token.IsCancellationRequested)
                  {
                    break;
                  }

                  string line = await reader.ReadLineAsync();
                  if (line != null)
                  {
                    Console.WriteLine(line);
                    var responseModel = JsonSerializer.Deserialize<LlamaResponseModel>(line);
                    await _aierHubContext.Clients.All.SendAsync("ReceiveText", responseModel.response);
                    Console.WriteLine(responseModel.response);
                  }
                }
              }
            }
          }
          else
          {
            // TODO: Raygun crash reporting
            Console.WriteLine($"Request failed with status code: {response.StatusCode}");
            string errorResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Error response:");
            Console.WriteLine(errorResponse);
            return StatusCode(StatusCodes.Status500InternalServerError);
          }
        }
        catch (Exception ex)
        {
          // TODO: Raygun crash reporting
          Console.WriteLine("An error occurred:");
          Console.WriteLine(ex.Message);
          return StatusCode(StatusCodes.Status500InternalServerError);
        }
      }

      return Ok();
    }

    private async Task EnsureModel()
    {
      var ollamaClient = new OllamaApiClient(new Uri("http://host.docker.internal:24606"));

      var models = await ollamaClient.ListLocalModels();
      if (models.Any(m => m.Name.StartsWith("llama3")))
      {
        return;
      }

      await ollamaClient.PullModel("llama3", async status =>
      {
        var percentage = status.Total == 0 ? 0 : status.Completed * 100 / (double)status.Total;
        // There are some initial messages in the stream that state the download has started, but do not mention the progress yet.
        // We do not want to send a message to the frontend for such cases to avoid the UI flickering once the actual percentage becomes known.
        if (percentage != 0)
        {
          await _aierHubContext.Clients.All.SendAsync("DownloadModelProgress", percentage);
        }
      });
    }

    public IActionResult CancelAIER()
    {
      if (_cancellationTokenSource != null)
      {
        _cancellationTokenSource.Cancel();
      }

      return Ok();
    }
  }
}
