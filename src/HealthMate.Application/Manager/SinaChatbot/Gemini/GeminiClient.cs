using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace HealthMate.Application.Manager.SinaChatbot
{
    public class GeminiClient
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiConfig _geminiConfig;

        public GeminiClient(HttpClient httpClient, IOptions<GeminiConfig> geminiConfig)
        {
            _httpClient = httpClient;
            _geminiConfig = geminiConfig.Value;
        }

        public async Task<string> AskAsync(string userPrompt)
        {
            string fullPrompt = BuildMedicalPrompt(userPrompt);
            string url = _geminiConfig.BaseUrl + _geminiConfig.ApiKey;

            var body = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = fullPrompt }
                        }
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(url, body);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return $"Error from Gemini API: {error}";
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            var reply = json
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return reply ?? "No reply from Gemini.";
        }

        private string BuildMedicalPrompt(string userPrompt)
        {
            return @$"
            You are an experienced medical doctor.
            You help answer medical questions with accuracy and professionalism.
            Only answer medical or healthcare-related questions.
            If the question is outside your medical knowledge, reply with:
            ""This is out of my knowledge Scope.""

            Question:
            {userPrompt}";
        }

        public async Task<string> AskRawPromptAsync(string fullPrompt)
        {
            string url = _geminiConfig.BaseUrl + _geminiConfig.ApiKey;

            var body = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = fullPrompt }
                        }
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(url, body);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return $"Error from Gemini API: {error}";
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            var reply = json
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return reply ?? "No reply from Gemini.";
        }

    }
}
