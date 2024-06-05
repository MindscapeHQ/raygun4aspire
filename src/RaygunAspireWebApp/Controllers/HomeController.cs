using Microsoft.AspNetCore.Mvc;
using RaygunAspireWebApp.Models;
using System.Diagnostics;
using System.Web;

namespace RaygunAspireWebApp.Controllers
{
  public class HomeController : Controller
  {
    private const int PageSize = 50;

    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
      _logger = logger;
    }

    public IActionResult Index()
    {
      var model = CreateErrorListViewModel(PageSize);

      return View(model);
    }

    public IActionResult ErrorList(int loaded)
    {
      var model = CreateErrorListViewModel(loaded + PageSize);

      return PartialView("_ErrorList", model);
    }

    private ErrorListViewModel CreateErrorListViewModel(int loadAmount)
    {
      if (Directory.Exists(IngestionController.ErrorsFolderPath))
      {
        var files = Directory.GetFiles(IngestionController.ErrorsFolderPath)
            .Select(filePath => new FileInfo(filePath))
            .OrderByDescending(filePath => filePath.CreationTime).ToList();

        loadAmount = Math.Min(loadAmount, files.Count);

        var errors = files.Take(loadAmount)
          .Select(ConvertFileInfoToErrorInstance)
          .ToList();

        return new ErrorListViewModel() { Errors = errors, Loaded = loadAmount, Total = files.Count };
      }

      return new ErrorListViewModel() { Errors = new List<ErrorInstanceRow>() };
    }
    
    private ErrorInstanceRow ConvertFileInfoToErrorInstance(FileInfo fileInfo)
    {
      var fileName = fileInfo.Name;
      var id = "";

      var index = fileName.IndexOf("-");
      if (index >= 0)
      {
        id = fileName.Substring(0, index);
        fileName = fileName.Substring(index + 1);
      }

      string decodedFilename = HttpUtility.UrlDecode(fileName);

      if (decodedFilename.EndsWith(".json"))
      {
        decodedFilename = decodedFilename.Substring(0, decodedFilename.Length - 5);
      }

      return new ErrorInstanceRow
      {
        Timestamp = fileInfo.LastWriteTime,
        Name = decodedFilename,
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
