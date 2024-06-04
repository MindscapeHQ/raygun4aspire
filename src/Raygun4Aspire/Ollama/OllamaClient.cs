using System.Text;
using System.Text.Json;
using Raygun4Aspire.Ollama.Models;

namespace Raygun4Aspire.Ollama
{
  internal class OllamaClient
  {
    public async Task<bool> HasModelAsync(string modelName, CancellationToken cancellationToken)
    {
      using (HttpClient client = new HttpClient())
      {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri("http://localhost:24606/api/tags"));

        var response = await client.SendAsync(requestMessage, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        var responseModel = JsonSerializer.Deserialize<TagsResponseModel>(responseBody);

        if (responseModel?.models != null)
        {
          return responseModel.models.Any(m => m.name != null && m.name.StartsWith(modelName));
        }

        return false;
      }
    }

    public async IAsyncEnumerable<double> PullModelAsync(string modelName, CancellationToken cancellationToken)
    {
      using (HttpClient client = new HttpClient())
      {
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri("http://localhost:24606/api/pull"));

        var json = JsonSerializer.Serialize(new { name = modelName, stream = true });
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        requestMessage.Content = content;

        var response = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        double percentage = 0;

        using (Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken))
        {
          using (StreamReader reader = new StreamReader(responseStream))
          {
            while (!reader.EndOfStream)
            {
              string? line = await reader.ReadLineAsync(cancellationToken);
              if (line != null)
              {
                Console.WriteLine(line);
                var responseModel = JsonSerializer.Deserialize<PullResponseModel>(line);
                if (responseModel != null)
                {
                  if (responseModel.total != 0)
                  {
                    percentage = responseModel.completed / (double)responseModel.total * 100;
                  }

                  yield return percentage;
                }
              }
            }
          }
        }
      }
    }
  }
}
