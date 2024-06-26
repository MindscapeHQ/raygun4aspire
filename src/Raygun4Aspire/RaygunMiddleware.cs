﻿using Microsoft.AspNetCore.Http;
using Mindscape.Raygun4Net.Breadcrumbs;

namespace Raygun4Aspire
{
  public class RaygunMiddleware
  {
    private readonly RequestDelegate _next;
    private readonly RaygunClient _client;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private const string UnhandledExceptionTag = "UnhandledException";

    public RaygunMiddleware(RequestDelegate next,
                            RaygunClient raygunClient,
                            IHttpContextAccessor httpContextAccessor)
    {
      _next = next;
      _client = raygunClient;
      _httpContextAccessor = httpContextAccessor;
    }

    public async Task Invoke(HttpContext httpContext)
    {
      httpContext.Request.EnableBuffering();

      if (RaygunBreadcrumbs.Storage is IContextAwareStorage storage)
      {
        storage.BeginContext();
      }

      try
      {
        // Let the request get invoked as normal
        await _next.Invoke(httpContext);
      }
      catch (Exception e)
      {
        // Capture the exception and send it to Raygun
        await _client.SendInBackground(e, tags: new List<string> { UnhandledExceptionTag }, context: _httpContextAccessor.HttpContext);
        throw;
      }
    }
  }
}
