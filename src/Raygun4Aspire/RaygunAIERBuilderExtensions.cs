using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting;
using System.Net.Sockets;

namespace Raygun4Aspire
{
  public static class RaygunAierBuilderExtensions
  {
    public static int DefaultHostPort = 24606;

    public static IResourceBuilder<RaygunAierResource> AddRaygunAIER(this IDistributedApplicationBuilder builder, string name = "Raygun-AIER", int? port = null)
    {
      var raygun = new RaygunAierResource(name);
      return builder.AddResource(raygun)
                    .WithAnnotation(new ContainerImageAnnotation { Image = "ollama/ollama", Tag = "rocm" })
                    //.WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", port: port ?? DefaultHostPort, targetPort: 11434))
                    .WithHttpEndpoint(port ?? DefaultHostPort, 11434, "raygunaier")
                    .WithVolume("raygun-aier", "/root/.ollama")
                    .PublishAsContainer();
    }
  }
}
