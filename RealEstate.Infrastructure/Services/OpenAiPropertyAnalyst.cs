using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using RealEstate.Domain.Interfaces;
using RealEstate.Application.Chat;

namespace RealEstate.Infrastructure.Services
{
    public class OpenAiPropertyAnalyst : IAiPropertyAnalyst
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _baseUrl;

        // Embeddings use a dedicated config section: most OpenRouter models/keys don't expose
        // a real /embeddings endpoint, so this defaults to talking to OpenAI directly. It falls
        // back to the OpenRouter key/url only if no dedicated Embeddings config is provided.
        private readonly string _embeddingsApiKey;
        private readonly string _embeddingsModel;
        private readonly string _embeddingsBaseUrl;

        public OpenAiPropertyAnalyst(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["OpenRouter:ApiKey"] ?? "";
            _model = configuration["OpenRouter:Model"] ?? "google/gemini-2.5-flash";
            _baseUrl = configuration["OpenRouter:BaseUrl"] ?? "https://openrouter.ai/api/v1";

            _embeddingsApiKey = configuration["Embeddings:ApiKey"] ?? _apiKey;
            _embeddingsModel = configuration["Embeddings:Model"] ?? "text-embedding-3-small";
            _embeddingsBaseUrl = configuration["Embeddings:BaseUrl"] ?? "https://api.openai.com/v1";
        }

        // 1. חילוץ תגיות מהטקסט בעזרת ה-AI
        public async Task<IEnumerable<string>> ExtractFeaturesAsync(string description)
        {
            var fallbackTags = new List<string> { "דירה", "נדלן" };

            var requestMessage = new
            {
                model = _model,
                max_tokens = 256, // מגביל את אורך התשובה כדי לא לחרוג ממכסת הקרדיטים (שגיאת 402)
                messages = new[]
                {
                    new { role = "system", content = "You are a real estate expert. Extract 3-5 comma-separated Hebrew tags from the property description (e.g., משופצת, מרווחת, נוף לים). Return ONLY the tags separated by commas, nothing else." },
                    new { role = "user", content = description }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions");
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            request.Content = JsonContent.Create(requestMessage);

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return fallbackTags;
                }

                var jsonResult = await response.Content.ReadFromJsonAsync<JsonElement>();
                var content = jsonResult.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                if (string.IsNullOrWhiteSpace(content)) return fallbackTags;

                return content.Split(',')
                              .Select(tag => tag.Trim())
                              .Where(tag => !string.IsNullOrEmpty(tag))
                              .ToList();
            }
            catch
            {
                return fallbackTags;
            }
        }

        // 2. Embedding אמיתי - זהו הבסיס לחיפוש הסמנטי (RAG). קורא ל-/embeddings
        //    ומחזיר את הווקטור בפועל, כדי שנוכל לדרג נכסים לפי דמיון סמנטי לשאילתת המשתמש.
        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrEmpty(_embeddingsApiKey))
            {
                // אין טקסט או שאין מפתח API מוגדר - מחזירים מערך ריק (ולא וקטור-אפס מזויף)
                // כדי שהקוד הקורא ידע בבירור שאין כאן embedding שאפשר להסתמך עליו.
                return Array.Empty<float>();
            }

            var requestBody = new
            {
                model = _embeddingsModel,
                input = text
            };

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_embeddingsBaseUrl}/embeddings");
                request.Headers.Add("Authorization", $"Bearer {_embeddingsApiKey}");
                request.Content = JsonContent.Create(requestBody);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Embeddings error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                    return Array.Empty<float>();
                }

                var result = await response.Content.ReadFromJsonAsync<EmbeddingsResponse>();
                var vector = result?.Data?.FirstOrDefault()?.Embedding;
                return vector ?? Array.Empty<float>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Embeddings exception: {ex.Message}");
                return Array.Empty<float>();
            }
        }

        // 3. ציון התאמה אמיתי בין שני טקסטים, מבוסס על דמיון קוסינוס בין ה-Embeddings שלהם.
        public async Task<double> GetMatchScoreAsync(string descriptionA, string descriptionB)
        {
            var vectorA = await GenerateEmbeddingAsync(descriptionA);
            var vectorB = await GenerateEmbeddingAsync(descriptionB);
            return VectorMath.CosineSimilarity(vectorA, vectorB);
        }

        private class EmbeddingsResponse
        {
            [JsonPropertyName("data")]
            public List<EmbeddingData>? Data { get; set; }
        }

        private class EmbeddingData
        {
            [JsonPropertyName("embedding")]
            public float[]? Embedding { get; set; }
        }
    }
}
