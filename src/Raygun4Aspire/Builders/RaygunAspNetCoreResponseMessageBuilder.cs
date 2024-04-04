using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;
using Mindscape.Raygun4Net;

namespace Raygun4Aspire.Builders
{
  public class RaygunAspNetCoreResponseMessageBuilder
  {
    public static RaygunResponseMessage Build(HttpContext? context)
    {
      if (context == null)
      {
        return new RaygunResponseMessage();
      }

      var httpResponseFeature = context.Features.Get<IHttpResponseFeature>();
      return new RaygunResponseMessage
      {
        StatusCode = context.Response.StatusCode,
        StatusDescription = httpResponseFeature?.ReasonPhrase
      };
    }
  }
}
