using System.Text.Json;
using Raygun4Aspire.Ollama.Models;

namespace Raygun4Aspire.Ollama
{
  internal class OllamaClient
  {
    public async Task<bool> HasModelAsync(string modelName)
    {
      using (HttpClient client = new HttpClient())
      {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri("http://host.docker.internal:24606/api/tags"));

        var response = await client.SendAsync(requestMessage);

        var responseBody = await response.Content.ReadAsStringAsync();

        var responseModel = JsonSerializer.Deserialize<TagsResponseModel>(responseBody);

        if (responseModel?.models != null)
        {
          return responseModel.models.Any(m => m.name != null && m.name.StartsWith(modelName));
        }

        return false;
      }
    }
  }
}
