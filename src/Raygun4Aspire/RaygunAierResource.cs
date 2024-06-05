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
      _endpointReference ??= new EndpointReference(this, "raygunaier");

    public ReferenceExpression ConnectionStringExpression =>
      ReferenceExpression.Create(
        $"http://{Endpoint.Property(EndpointProperty.Host)}:{Endpoint.Property(EndpointProperty.Port)}"
      );
  }
}
