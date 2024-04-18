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
                    .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", port: port ?? DefaultHostPort, containerPort: 8080))
                    .WithAnnotation(new ContainerImageAnnotation { Image = "raygunowner/raygun-aspire-portal", Tag = "1.0.0-preview.1.0.0" })
                    .WithVolumeMount("raygun-data", "/app/raygun")
                    .PublishAsContainer();
    }
  }
}
