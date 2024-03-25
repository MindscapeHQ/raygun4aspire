using System.Net.Sockets;

namespace Raygun4Aspire
{
  // TODO: this name will likely be simplified
  public static class RaygunAspireWebAppBuilderExtensions
  {
    public static IResourceBuilder<RaygunAspireWebAppResource> AddRaygun(this IDistributedApplicationBuilder builder, string name, int? port = null)
    {
      var raygun = new RaygunAspireWebAppResource(name);
      return builder.AddResource(raygun)
                    .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", port: port, containerPort: 8080))
                    .WithAnnotation(new ContainerImageAnnotation { Image = "raygunaspirewebapp", Tag = "latest" })
                    .WithVolumeMount("raygun-data", "/app/raygun")
                    .PublishAsContainer();
    }
  }
}
