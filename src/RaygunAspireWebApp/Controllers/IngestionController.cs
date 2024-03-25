using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace RaygunAspireWebApp.Controllers
{
    public class IngestionController : Controller
    {
        public async Task<IActionResult> Entries()
        {
            Console.WriteLine("Yayy, received a Raygun crash report");

            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                string requestBody = await reader.ReadToEndAsync();
                Console.WriteLine(requestBody);

                var raygunMessage = JsonSerializer.Deserialize<Mindscape.Raygun4Net.RaygunMessage>(requestBody);

                var info = Directory.CreateDirectory("/app/raygun/errors");

                Console.WriteLine("FULL FILE PATH:");
                Console.WriteLine(info.FullName);

                var message = raygunMessage.Details.Error.Message;
                var uniqueSlug = DateTime.UtcNow.Ticks;

                System.IO.File.WriteAllText($"/app/raygun/errors/{uniqueSlug}|{message}.json", requestBody);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return Accepted();
        }
    }
}
