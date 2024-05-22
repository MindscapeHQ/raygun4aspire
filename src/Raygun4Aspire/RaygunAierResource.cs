namespace Aspire.Hosting.ApplicationModel
{
  public class RaygunAierResource(string name) : ContainerResource(name), IResourceWithConnectionString
  {
    private EndpointReference? _endpointReference;

    public EndpointReference Endpoint =>
        _endpointReference ??= new(this, "http");

    public ReferenceExpression ConnectionStringExpression =>
      ReferenceExpression.Create(
            //$"http://{Endpoint.Property(EndpointProperty.Host)}:{Endpoint.Property(EndpointProperty.Port)}"
            $"http://localhost:24606"
        );
  }
}
