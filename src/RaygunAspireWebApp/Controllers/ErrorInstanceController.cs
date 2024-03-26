using Microsoft.AspNetCore.Mvc;
using RaygunAspireWebApp.Models;
using System.Text.Json;

namespace RaygunAspireWebApp.Controllers
{
  public class ErrorInstanceController : Controller
  {
    public IActionResult Details(string id)
    {
      var folderPath = "/app/raygun/errors";
      // TODO: do an exact match where we expect the id to be in the file name (in case an error message happens to contain the id of a different error)
      var filePaths = Directory.GetFiles(folderPath).Where(n => n.Contains(id)).ToList();

      if (filePaths.Count == 1)
      {
        var fileContents = System.IO.File.ReadAllText(filePaths[0]);
        return View(new ErrorInstanceViewModel { RawPayload = fileContents });
      }

      return View(new ErrorInstanceViewModel { RawPayload = "Could not find error report" });
    }

    public IActionResult TabContent(string tab, string model)
    {
      var tabModel = JsonSerializer.Deserialize<ErrorInstanceViewModel>(model, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

      switch (tab)
      {
        case "summary":
          return PartialView("Tabs/_Summary", tabModel);
        case "rawdata":
          return PartialView("Tabs/_RawData", tabModel);
        default:
          return PartialView("Tabs/_Summary", tabModel);
      }
    }
  }
}
