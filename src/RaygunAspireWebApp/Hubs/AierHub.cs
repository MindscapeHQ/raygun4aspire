using Microsoft.AspNetCore.SignalR;

namespace RaygunAspireWebApp.Hubs
{
  public class AierHub : Hub
  {
    public async Task AppendText(string text)
    {
      await Clients.All.SendAsync("ReceiveText", text);
    }
  }
}
