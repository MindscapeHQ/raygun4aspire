using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting;
using System.Net.Sockets;
using System.Collections.Immutable;
using Aspire.Hosting.Lifecycle;

namespace Raygun4Aspire
{
  public static class RaygunAierBuilderExtensions
  {
    //public static int DefaultHostPort = 24606;

    public static IResourceBuilder<RaygunAierResource> AddRaygunAIER(this IDistributedApplicationBuilder builder,
      string name = "Raygun-AIER", int? port = null)
    {
      builder.Services.TryAddLifecycleHook<RaygunAierResourceLifecycleHook>();
      var raygun = new RaygunAierResource(name);
      return builder.AddResource(raygun)
        .WithAnnotation(new ContainerImageAnnotation { Image = "ollama/ollama", Tag = "latest" })
        //.WithContainerRuntimeArgs("--device", "/dev/kfd")
        //.WithArgs("pull", "llama3")
        //.WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", port: port ?? DefaultHostPort, targetPort: 11434))
        .WithHttpEndpoint(port, 11434, "raygunaier")
        .WithVolume("raygun-aier", "/root/.ollama")
        /*.WithInitialState(new CustomResourceSnapshot
        {
          ResourceType = "External container",
          State = "Starting",
          Properties = ImmutableArray.Create(new ResourcePropertySnapshot(CustomResourceKnownProperties.Source, "Custom"))
        })*/
        .ExcludeFromManifest()
        .PublishAsContainer();
    }
  }
}
