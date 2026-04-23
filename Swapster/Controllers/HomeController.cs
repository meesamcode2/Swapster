using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;

namespace Swapster.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        // ================= CHATBOT API =================
        [HttpPost]
        public async Task<IActionResult> Chat(string message)
        {
            using (var client = new HttpClient())
            {
                var payload = new { message = message };

                var json = System.Text.Json.JsonSerializer.Serialize(payload);

                var content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync("http://127.0.0.1:5000/chat", content);

                var result = await response.Content.ReadAsStringAsync();

                return Content(result, "application/json");
            }
        }
    }
}