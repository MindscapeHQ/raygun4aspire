using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting;
using System.Net.Sockets;

namespace Raygun4Aspire
{
  public static class RaygunAIERBuilderExtensions
  {
    public static int DefaultHostPort = 24606;

    public static IResourceBuilder<RaygunAspireWebAppResource> AddRaygunAIER(this IDistributedApplicationBuilder builder, string name = "Raygun-AIER", int? port = null)
    {
      var raygun = new RaygunAspireWebAppResource(name);
      return builder.AddResource(raygun)
                    .WithAnnotation(new ContainerImageAnnotation { Image = "ollama/ollama", Tag = "rocm" })
                    .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", port: port ?? DefaultHostPort, targetPort: 11434))
                    .WithVolume("raygun-aier", "/root/.ollama")
                    .PublishAsContainer();
    }
  }
}
