using Mindscape.Raygun4Net;

namespace RaygunAspireWebApp.Models
{
  public class ErrorInstanceViewModel
  {
    public string RawPayload { get; set; }
    public RaygunMessage RaygunMessage { get; set; }
  }
}
