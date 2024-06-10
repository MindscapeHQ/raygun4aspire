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
        Console.WriteLine(ex.ToString());
        await _raygunClient.SendInBackground(ex);
        return StatusCode(StatusCodes.Status500InternalServerError);
      }

      var prompt = BuildPrompt();

      try
      {
        await _ollamaClient.StreamCompletion(prompt, null, response =>
        {
          if (response?.Response != null)
          {
            _aierHubContext.Clients.All.SendAsync("ReceiveText", response.Response);
          }
        }, _cancellationTokenSource.Token);
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
        await _raygunClient.SendInBackground(ex);
        return StatusCode(StatusCodes.Status500InternalServerError);
      }

      return Ok();
    }

    private async Task EnsureModel()
    {
      var models = await _ollamaClient.ListLocalModels();
      if (!models.Any(m => m.Name.StartsWith(Constants.AiModel)))
      {
        // If the model has not been downloaded yet, then kick off that process.
        // If the model is already downloading, then this will pick up the progress of the existing download job:
        await _ollamaClient.PullModel(Constants.AiModel, status =>
        {
          var percentage = status.Total == 0 ? 0 : status.Completed * 100 / (double)status.Total;
          // There are some initial messages in the stream that state the download has started, but do not mention the progress yet.
          // We do not want to send a message to the frontend for such cases to avoid the UI flickering once the actual percentage becomes known.
          if (percentage != 0)
          {
            _aierHubContext.Clients.All.SendAsync("DownloadModelProgress", percentage);
          }
        });

        // Straight after the download completes, wait until the model becomes available:
        while (true)
        {
          models = await _ollamaClient.ListLocalModels();
          if (models.Any(m => m.Name.StartsWith(Constants.AiModel)))
          {
            return;
          }

          await Task.Delay(TimeSpan.FromSeconds(1));
        }
      }
    }

    private string BuildPrompt()
    {
      var prompt = "My .NET Aspire application has encountered the following exception. Briefly and professionally explain how I should look into fixing it.";

      var modelString = HttpContext.Session.GetString("Model");
      var model = JsonSerializer.Deserialize<ErrorInstanceViewModel>(modelString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new RaygunIdentifierMessageConverter() } });

      var detailsModel = model?.RaygunMessage?.Details;
      var errorModel = GetInnerMostError(detailsModel?.Error);

      var exceptionString = !string.IsNullOrWhiteSpace(errorModel?.ClassName) ? $"{errorModel.ClassName}: " : "";
      exceptionString += errorModel?.Message ?? "";

      if (string.IsNullOrWhiteSpace(exceptionString))
      {
        exceptionString = "Unknown Exception";
      }

      prompt += $"\r\nThe exception is: {exceptionString}";

      // If present, provide the first line of the stack trace:
      if (errorModel?.StackTrace?.Length > 0)
      {
        var errorSite = errorModel.StackTrace[0];
        string? errorSiteString = null;
        if (!string.IsNullOrWhiteSpace(errorSite.ClassName) && !string.IsNullOrWhiteSpace(errorSite.MethodName))
        {
          errorSiteString = $"{errorSite.ClassName}.{errorSite.MethodName}";
        }
        else if (errorSite.FileName != null)
        {
          errorSiteString = errorSite.FileName;
        }

        if (errorSiteString != null)
        {
          prompt += $"\r\nThe exception occurred here: {errorSiteString}";
        }
      }

      // If present, provide the request details:
      var requestModel = detailsModel?.Request;
      if (!string.IsNullOrWhiteSpace(requestModel?.Url))
      {
        var requestString = string.IsNullOrWhiteSpace(requestModel.HttpMethod) ? "" : $"{requestModel.HttpMethod} ";
        requestString += requestModel.Url;

        prompt += $"\r\nThe exception occurred during this web request: {requestString}";
      }

      return prompt;
    }

    private static RaygunErrorMessage? GetInnerMostError(RaygunErrorMessage? errorModel)
    {
      if (errorModel?.InnerError != null)
      {
        return GetInnerMostError(errorModel.InnerError);
      }

      if (errorModel?.InnerErrors?.Length > 0 && errorModel.InnerErrors[0] != null)
      {
        return GetInnerMostError(errorModel.InnerErrors[0]);
      }

      return errorModel;
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
