using Microsoft.AspNetCore.Http;
using Mindscape.Raygun4Net;
using Raygun4Aspire.Builders;
using System.Collections;
using System.Diagnostics;

namespace Raygun4Aspire
{
  public class RaygunClient : RaygunClientBase
  {
    public RaygunClient(RaygunSettings settings, IRaygunUserProvider userProvider)
      : base(settings, userProvider)
    {
    }

    protected Lazy<RaygunSettings> Settings => new(() => (RaygunSettings)_settings);

    protected override bool CanSend(RaygunMessage? message)
    {
      if (message?.Details?.Response == null)
      {
        return true;
      }

      var settings = Settings.Value;
      if (settings.ExcludedStatusCodes == null)
      {
        return true;
      }

      return !settings.ExcludedStatusCodes.Contains(message.Details.Response.StatusCode);
    }

    /// <summary>
    /// Asynchronously transmits an exception to Raygun with optional Http Request data.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">(Optional) A List&lt;string&gt; of tags to associate with the exception.</param>
    /// <param name="userCustomData">(Optional) A dictionary of any additional contextual data that could help you understand an exception occurrence.</param>
    /// <param name="userInfo">(Optional) A <see cref="RaygunIdentifierMessage" /> of optional user information.</param>
    /// <param name="context">(Optional) The current HttpContext of a request if applicable.</param>
    public async Task SendInBackground(Exception exception, IList<string>? tags = null, IDictionary? userCustomData = null, RaygunIdentifierMessage? userInfo = null, HttpContext? context = null)
    {
      if (CanSend(exception))
      {
        // We need to process the Request on the current thread,
        // otherwise it will be disposed while we are using it on the other thread.
        // BuildRequestMessage relies on ReadFormAsync, so we need to await it to ensure it's processed before continuing.
        var currentRequestMessage = await RaygunRequestMessageBuilder.Build(context, Settings.Value);
        var currentResponseMessage = RaygunResponseMessageBuilder.Build(context);

        var exceptions = StripWrapperExceptions(exception);

        foreach (var ex in exceptions)
        {
          var msg = await BuildMessage(ex, tags, userCustomData, userInfo, customiseMessage: msg =>
          {
            msg.Details.Request = currentRequestMessage;
            msg.Details.Response = currentResponseMessage;
          });

          if (!Enqueue(msg))
          {
            Debug.WriteLine("Could not add message to background queue. Dropping exception: {0}", ex);
          }
        }

        FlagAsSent(exception);
      }
    }
  }
}
