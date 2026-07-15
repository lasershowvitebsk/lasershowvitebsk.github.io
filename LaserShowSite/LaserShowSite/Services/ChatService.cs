using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace LaserShowSite.Services
{
    public class ChatService
    {
        private readonly HttpClient _http;
        private const string WorkerBase =
            "https://laser-api.vitalitalipski.workers.dev";

        public ChatService(HttpClient http)
        {
            _http = http;
        }

        // Обычный вопрос через LLM
        public async Task<string> GetBotReplyAsync(
            string userMessage,
            string language = "ru")
        {
            try
            {
                var payload = new { message = userMessage, language };
                var response = await _http.PostAsJsonAsync(
                    $"{WorkerBase}/chat", payload
                );

                if (!response.IsSuccessStatusCode)
                    return GetClientFallback(language);

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var reply = doc.RootElement
                    .GetProperty("reply")
                    .GetString();

                return reply ?? GetClientFallback(language);
            }
            catch
            {
                return GetClientFallback(language);
            }
        }

        // Чипс — готовый ответ без LLM
        public async Task<string> GetChipReplyAsync(
            string chipId,
            string language = "ru")
        {
            try
            {
                var payload = new { message = chipId, chipId, language };
                var response = await _http.PostAsJsonAsync(
                    $"{WorkerBase}/chat", payload
                );

                if (!response.IsSuccessStatusCode)
                    return GetClientFallback(language);

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var reply = doc.RootElement
                    .GetProperty("reply")
                    .GetString();

                return reply ?? GetClientFallback(language);
            }
            catch
            {
                return GetClientFallback(language);
            }
        }

        // Заглушка если сеть недоступна
        private static string GetClientFallback(string language)
        {
            return language == "ru"
                ? "Не удалось получить ответ. Проверьте соединение или попробуйте позже."
                : "Could not get a response. Check your connection or try again later.";
        }
    }
}