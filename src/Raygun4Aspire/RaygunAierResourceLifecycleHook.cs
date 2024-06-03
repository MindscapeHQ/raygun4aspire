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
        try
        {
          var ollamaClient = new OllamaClient();

          await _notificationService.PublishUpdateAsync(resource, state => state with { State = new ResourceStateSnapshot("Checking model", KnownResourceStateStyles.Info) });
          var hasModel = await ollamaClient.HasModelAsync("llama3");

          if (!hasModel)
          {
            await _notificationService.PublishUpdateAsync(resource, state => state with { State = new ResourceStateSnapshot("Downloading model", KnownResourceStateStyles.Info) });

            await foreach (var percentage in ollamaClient.PullModelAsync("llama3"))
            {
              var percentageState = $"Downloading model {percentage:N0} percent";
              await _notificationService.PublishUpdateAsync(resource, state => state with { State = new ResourceStateSnapshot(percentageState, KnownResourceStateStyles.Info) });
            }
          }

          await _notificationService.PublishUpdateAsync(resource, state => state with { State = new ResourceStateSnapshot("Ready", KnownResourceStateStyles.Success) });
        }
        catch (Exception ex)
        {
          await _notificationService.PublishUpdateAsync(resource, state => state with { State = new ResourceStateSnapshot(ex.Message, KnownResourceStateStyles.Error) });
        }
        
      }, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
      _tokenSource.Cancel();
      return default;
    }
  }
}
