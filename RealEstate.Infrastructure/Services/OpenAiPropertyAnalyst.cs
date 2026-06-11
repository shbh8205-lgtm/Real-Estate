// using OpenAI.Embeddings;
// using RealEstate.Domain.Interfaces;

// public class OpenAiPropertyAnalyst : IAiPropertyAnalyst
// {
//     private readonly EmbeddingClient _embeddingClient;

//     public OpenAiPropertyAnalyst()
//     {
//         //_embeddingClient = new EmbeddingClient("text-embedding-3-small", apiKey);
//     }

//     public async Task<float[]> GenerateEmbeddingAsync(string text)
//     {
//         // כרגע אנחנו מחזירים מערך דמיוני (Mock) כדי שהמערכת לא תקרוס
//         // בהמשך, כשיהיה מפתח, נחזיר את הקריאה האמיתית ל-OpenAI
//         return await Task.FromResult(new float[] { 0.1f, 0.2f, 0.3f });
//     }
//     // שאר המתודות (ExtractFeaturesAsync וכו') יישארו כרגע עם מימוש דמי
//     public async Task<IEnumerable<string>> ExtractFeaturesAsync(string description) =>
//         new List<string> { "מוארת", "מרפסת" };

//     public async Task<double> GetMatchScoreAsync(string prop, string req) => 0.85;
// }




using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using RealEstate.Domain.Interfaces;

namespace RealEstate.Infrastructure.Services
{
    public class OpenAiPropertyAnalyst : IAiPropertyAnalyst
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _baseUrl;

        public OpenAiPropertyAnalyst(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["OpenRouter:ApiKey"];
            _model = configuration["OpenRouter:Model"] ?? "google/gemini-2.5-flash";
            _baseUrl = configuration["OpenRouter:BaseUrl"] ?? "https://openrouter.ai/api/v1";
        }

        // 1. חילוץ תגיות מהטקסט בעזרת ה-AI
        public async Task<IEnumerable<string>> ExtractFeaturesAsync(string description)
        {
            var fallbackTags = new List<string> { "דירה", "נדלן" };

            var requestMessage = new
            {
                model = _model,
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

        // 2. תיקון: הוספת המתודה שהייתה חסרה לחלוטין לפי צילום המסך
        public async Task<float[]> GenerateEmbeddingAsync(string description)
        {
            // מחזיר מערך ריק זמני (Mock) כדי שלא יקרוס ויקיים את תנאי הממשק
            return await Task.FromResult(new float[1536]);
        }

        // 3. תיקון: שינוי סוג ההחזרה מ-float ל-double (Task<double>) כפי שהקומפיילר דרש
        public async Task<double> GetMatchScoreAsync(string descriptionA, string descriptionB)
        {
            // מחזיר ציון דמה קבוע של 85% התאמה
            return await Task.FromResult(0.85);
        }
    }
}