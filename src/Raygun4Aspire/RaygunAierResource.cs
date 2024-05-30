namespace Aspire.Hosting.ApplicationModel
{
  public class RaygunAierResource : ContainerResource, IResourceWithConnectionString
  {
    private EndpointReference? _endpointReference;

    public RaygunAierResource(string name)
      : base(name)
    {
    }

    public EndpointReference Endpoint =>
        _endpointReference ??= new(this, "http");

    public ReferenceExpression ConnectionStringExpression =>
      ReferenceExpression.Create(
            //$"http://{Endpoint.Property(EndpointProperty.Host)}:{Endpoint.Property(EndpointProperty.Port)}"
            $"http://localhost:24606"
        );
  }
}
