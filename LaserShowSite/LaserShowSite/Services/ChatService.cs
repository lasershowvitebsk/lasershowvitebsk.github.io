using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace LaserShowSite.Services
{
    public class ChatService
    {
        private readonly HttpClient _http;
        private const string WorkerBase = "https://laser-api.vitalitalipski.workers.dev"; // замените позже

        public ChatService(HttpClient http)
        {
            _http = http;
        }

        public async Task<string> GetBotReplyAsync(string userMessage)
        {
            var request = new { prompt = userMessage };
            var response = await _http.PostAsJsonAsync($"{WorkerBase}/chat", request);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);
            var reply = doc.RootElement.GetProperty("reply").GetString();
            return reply ?? "Извините, я не понял запрос.";
        }
    }
}