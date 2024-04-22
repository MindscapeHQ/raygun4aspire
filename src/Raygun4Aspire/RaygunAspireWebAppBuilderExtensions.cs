using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using System.Net.Sockets;

namespace Raygun4Aspire
{
  public static class RaygunAspireWebAppBuilderExtensions
  {
    public static int DefaultHostPort = 24605;

    public static IResourceBuilder<RaygunAspireWebAppResource> AddRaygun(this IDistributedApplicationBuilder builder, string name = "Raygun", int? port = null)
    {
      var raygun = new RaygunAspireWebAppResource(name);
      return builder.AddResource(raygun)
                    .WithAnnotation(new ContainerImageAnnotation { Image = "raygunowner/raygun-aspire-portal", Tag = "1.0.0-preview.1.0.0" })
                    .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", port: port ?? DefaultHostPort, targetPort: 8080))
                    .WithBindMount("raygun-data", "/app/raygun")
                    .PublishAsContainer();
    }
  }
}
