using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Raygun4Aspire.Ollama;

namespace Raygun4Aspire
{
  internal class RaygunAierResourceLifecycleHook : IDistributedApplicationLifecycleHook, IAsyncDisposable
  {
    private readonly ResourceNotificationService _notificationService;

    private readonly CancellationTokenSource _tokenSource = new();

    public RaygunAierResourceLifecycleHook(ResourceNotificationService notificationService)
    {
      _notificationService = notificationService;
    }

    public Task AfterResourcesCreatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
      foreach (var resource in appModel.Resources.OfType<RaygunAierResource>())
      {
        DownloadModel(resource, _tokenSource.Token);
      }

      return Task.CompletedTask;
    }

    private void DownloadModel(RaygunAierResource resource, CancellationToken cancellationToken)
    {
      _ = Task.Run(async () =>
      {
        //Thread.Sleep(5000);
        bool hasModel = false;
        try
        {
          var ollamaClient = new OllamaClient();
          hasModel = await ollamaClient.HasModelAsync("llama3");

          if (!hasModel)
          {
            await foreach (var str in ollamaClient.PullModelAsync("llama3"))
            {
              await _notificationService.PublishUpdateAsync(resource, state => state with { State = new ResourceStateSnapshot(str, KnownResourceStateStyles.Info) });
            }

            await _notificationService.PublishUpdateAsync(resource, state => state with { State = new ResourceStateSnapshot("Running", KnownResourceStateStyles.Success) });
          }
        }
        catch (Exception ex)
        {
          await _notificationService.PublishUpdateAsync(resource, state => state with { State = ex.Message });
          return;
        }

        //await _notificationService.PublishUpdateAsync(resource, state => state with { State = hasModel ? "MODEL" : "NOPE",  });
      }, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
      _tokenSource.Cancel();
      return default;
    }
  }
}
