using System.Net.Http;
using System.Net.Http.Json;
using LaserShowSite.Models;

namespace LaserShowSite.Services
{
    public class RequestService
    {
        private readonly HttpClient _http;
        private const string WorkerBase = "https://laser-api.vitalitalipski.workers.dev";

        public RequestService(HttpClient http)
        {
            _http = http;
        }

        public async Task<bool> SubmitRequestAsync(RequestModel request)
        {
            var response = await _http.PostAsJsonAsync($"{WorkerBase}/request", request);
            return response.IsSuccessStatusCode;
        }
    }
}