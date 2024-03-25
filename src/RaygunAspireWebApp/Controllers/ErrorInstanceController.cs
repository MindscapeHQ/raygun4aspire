using Microsoft.AspNetCore.Mvc;
using RaygunAspireWebApp.Models;

namespace RaygunAspireWebApp.Controllers
{
  public class ErrorInstanceController : Controller
  {
    public IActionResult Details(string id)
    {
      var folderPath = "/app/raygun/errors";
      // TODO: do an exact match where we expect the id to be in the file name
      var filePaths = Directory.GetFiles(folderPath).Where(n => n.Contains(id)).ToList();

      if (filePaths.Count == 1)
      {
        var fileContents = System.IO.File.ReadAllText(filePaths[0]);
        return View(new ErrorInstanceViewModel { RawPayload = fileContents });
      }

      return View(new ErrorInstanceViewModel { RawPayload = "Could not find error report" });
    }
  }
}
