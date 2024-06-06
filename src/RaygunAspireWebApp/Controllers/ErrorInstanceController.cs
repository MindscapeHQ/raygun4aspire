using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Mindscape.Raygun4Net;
using Mindscape.Raygun4Net.AspNetCore;
using RaygunAspireWebApp.Hubs;
using RaygunAspireWebApp.Models;
using System.Text.Json;
using OllamaSharp;

namespace RaygunAspireWebApp.Controllers
{
  public class ErrorInstanceController : Controller
  {
    private RaygunClient _raygunClient;
    private IHubContext<AierHub> _aierHubContext;
    private IOllamaApiClient? _ollamaClient;

    private static CancellationTokenSource? _cancellationTokenSource;

    public ErrorInstanceController(RaygunClient raygunClient, IHubContext<AierHub> aierHubContext, IOllamaApiClient? ollamaClient = null)
    {
      _raygunClient = raygunClient;
      _aierHubContext = aierHubContext;
      _ollamaClient = ollamaClient;
    }

    public IActionResult Details(string id)
    {
      var filePaths = Directory.GetFiles(IngestionController.ErrorsFolderPath).Where(n => Path.GetFileName(n).StartsWith(id)).ToList();

      if (filePaths.Count == 1)
      {
        var fileContents = System.IO.File.ReadAllText(filePaths[0]);

        var raygunMessage = JsonSerializer.Deserialize<RaygunMessage>(fileContents, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new RaygunIdentifierMessageConverter() } });

        var model = new ErrorInstanceViewModel { RaygunMessage = raygunMessage, AierEnabled = _ollamaClient != null};

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

    public async Task<IActionResult> StartAier()
    {
      if (_ollamaClient == null)
      {
        return StatusCode(StatusCodes.Status500InternalServerError);
      }

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

      var errorModel = model?.RaygunMessage?.Details?.Error;

      var exceptionString = !string.IsNullOrWhiteSpace(errorModel.ClassName) ? $"{errorModel.ClassName}: " : "";
      exceptionString += errorModel.Message;

      string question = "My .NET Aspire application has encountered the following exception. Briefly explain how I should look into fixing it.\r\nThe exception is: " + exceptionString;

      try
      {
        await _ollamaClient.StreamCompletion(question, null, async response =>
        {
          if (response.Response != null)
          {
            await _aierHubContext.Clients.All.SendAsync("ReceiveText", response.Response);
          }
        }, _cancellationTokenSource.Token);
      }
      catch (Exception ex)
      {
        // TODO: Raygun crash reporting
        Console.WriteLine("An error occurred:");
        Console.WriteLine(ex.Message);
        return StatusCode(StatusCodes.Status500InternalServerError);
      }

      return Ok();
    }

    private async Task EnsureModel()
    {
      var models = await _ollamaClient.ListLocalModels();
      if (models.Any(m => m.Name.StartsWith(Constants.AiModel)))
      {
        return;
      }

      await _ollamaClient.PullModel(Constants.AiModel, async status =>
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

    public IActionResult CancelAier()
    {
      if (_cancellationTokenSource != null)
      {
        _cancellationTokenSource.Cancel();
      }

      return Ok();
    }
  }
}
