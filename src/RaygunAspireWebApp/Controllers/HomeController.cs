using Microsoft.AspNetCore.Mvc;
using RaygunAspireWebApp.Models;
using System.Diagnostics;

namespace RaygunAspireWebApp.Controllers
{
  public class HomeController : Controller
  {
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
      _logger = logger;
    }

    public IActionResult Index()
    {
      var folderPath = "/app/raygun/errors";
      if (Directory.Exists(folderPath))
      {
        var files = Directory.GetFiles(folderPath)
            .Select(filePath => new FileInfo(filePath))
            .OrderByDescending(filePath => filePath.CreationTime)
            .Select(ConvertFileInfoToErrorInstance)
            .ToList();

        return View(files);
      }

      return View(new List<ErrorInstanceRow>());
    }

    private ErrorInstanceRow ConvertFileInfoToErrorInstance(FileInfo fileInfo)
    {
      var fileName = fileInfo.Name;
      var id = "";

      var index = fileName.IndexOf("|");
      if (index >= 0)
      {
        id = fileName.Substring(0, index);
        fileName = fileName.Substring(index + 1);
      }

      if (fileName.EndsWith(".json"))
      {
        fileName = fileName.Substring(0, fileName.Length - 5);
      }

      return new ErrorInstanceRow
      {
        Timestamp = fileInfo.LastWriteTime,
        Name = fileName,
        Id = id
      };
    }

    public IActionResult Privacy()
    {
      return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
      return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
  }
}
