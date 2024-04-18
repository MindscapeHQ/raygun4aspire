using Microsoft.AspNetCore.Http;
using Mindscape.Raygun4Net;
using System.Security.Claims;

namespace Raygun4Aspire
{
  public class DefaultRaygunUserProvider : IRaygunUserProvider
  {
    private readonly IHttpContextAccessor _contextAccessor;

    public DefaultRaygunUserProvider(IHttpContextAccessor contextAccessor)
    {
      _contextAccessor = contextAccessor;
    }

    public RaygunIdentifierMessage? GetUser()
    {
      var ctx = _contextAccessor.HttpContext;

      if (ctx == null)
      {
        return null;
      }

      var identity = ctx.User.Identity as ClaimsIdentity;

      if (identity?.IsAuthenticated == true)
      {
        var email = identity.FindFirst(ClaimTypes.Email)?.Value ?? identity.Name;

        return new RaygunIdentifierMessage(email)
        {
          IsAnonymous = false,
          Email = email,
          FullName = identity.Name
        };
      }

      return null;
    }
  }
}
