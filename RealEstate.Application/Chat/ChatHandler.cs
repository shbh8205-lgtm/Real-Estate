using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using RealEstate.Domain.Interfaces;
using RealEstate.Domain.Entities;
using RealEstate.Application.Properties.Dtos;

namespace RealEstate.Application.Chat
{

    // מחלקה פנימית לקבלת התשובה מה-AI 
    public class OpenRouterChatResponse
    {
        public string? reply { get; set; }
        public string? city { get; set; }
        public long? maxPrice { get; set; }
    }

    public class ChatHandler : IRequestHandler<ChatQuery, ChatReply>
    {
        private readonly HttpClient _httpClient;
        private readonly IAsyncRepository<Property> _propertyRepository;
        private readonly IMemoryCache _cache;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _baseUrl;

        public ChatHandler(
            HttpClient httpClient,
            IAsyncRepository<Property> propertyRepository,
            IConfiguration configuration,
            IMemoryCache cache)
        {
            _httpClient = httpClient;
            _propertyRepository = propertyRepository;
            _cache = cache;
            _apiKey = configuration["OpenRouter:ApiKey"] ?? "";
            _model = configuration["OpenRouter:Model"] ?? "google/gemini-2.5-flash";
            _baseUrl = configuration["OpenRouter:BaseUrl"] ?? "https://openrouter.ai/api/v1";
        }

        public async Task<ChatReply> Handle(ChatQuery request, CancellationToken cancellationToken)
        {
            // הגדלת הקשיחות של ה-Prompt כדי שלא יצא מהתפקיד שלו לעולם
            var systemPrompt = @"You are a real estate search engine API. 
    Your ONLY job is to return a strict JSON object. NEVER reply with plain text or explanations.
    
    Expected JSON Structure:
    {
        ""reply"": ""An encouraging response in Hebrew listing what you found or asking for clarification"",
        ""city"": ""The city name extracted in Hebrew OR English as it might appear in a database (e.g., 'תל אביב' or 'Tel Aviv')"",
        ""maxPrice"": 3000000
    }

    Rules:
    - If the user asks 'where are the properties?' or 'show me properties', use the previous conversation context to maintain the city and price filters in the JSON.
    - NEVER say you don't have access to data. You DO have access through the system.";

            var cacheKey = $"chat_{request.ConversationId}";
            var history = _cache.Get<List<object>>(cacheKey) ?? new List<object>
    {
        new { role = "system", content = systemPrompt }
    };

            history.Add(new { role = "user", content = request.Message });

            var requestMessage = new
            {
                model = _model,
                //response_format = new { type = "json_object" }, // מכריח את המודל להחזיר JSON תקין
                messages = history.ToArray()
            };

            try
            {
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions");
                httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");
                httpRequest.Content = JsonContent.Create(requestMessage);

                var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    return GetFallbackReply("מתנצל, יש לי קושי זמני להתחבר לשרת הבינה המלאכותית.");
                }

                var jsonResult = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
                var rawContent = jsonResult.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                if (string.IsNullOrWhiteSpace(rawContent))
                {
                    return GetFallbackReply("לא קיבלתי תשובה מהמערכת. נסה לנסח מחדש.");
                }

                // המודל לעיתים עוטף את ה-JSON ב-```json ... ``` או מוסיף טקסט מסביב.
                // ננקה את העטיפה ונחלץ את אובייקט ה-JSON לפני הפענוח.
                var aiData = TryParseAiResponse(rawContent);

                if (aiData == null)
                {
                    // לא הצלחנו לפענח JSON - נחזיר את הטקסט שהמודל כתב כתשובה רגילה,
                    // כדי שהמשתמש יקבל מענה במקום שגיאה גנרית.
                    history.Add(new { role = "assistant", content = rawContent });
                    _cache.Set(cacheKey, history, TimeSpan.FromMinutes(20));
                    return GetFallbackReply(rawContent.Trim());
                }

                // שמירת תשובת ה-AI (הטקסט שהוא ייצר עבור המשתמש) בהיסטוריה
                history.Add(new { role = "assistant", content = rawContent });
                _cache.Set(cacheKey, history, TimeSpan.FromMinutes(20));

                // שליפת הדירות מהמאגר
                var allProperties = await _propertyRepository.ListAllAsync();
                var filteredProperties = allProperties.AsQueryable();

                // סינון חכם - בודק גם אנגלית וגם עברית או חלקי מילים
                if (!string.IsNullOrEmpty(aiData.city))
                {
                    var cityTarget = aiData.city.Trim();
                    filteredProperties = filteredProperties.Where(p => p.Address.Contains(cityTarget));
                }

                if (aiData.maxPrice.HasValue && aiData.maxPrice.Value > 0)
                {
                    filteredProperties = filteredProperties.Where(p => p.Price <= aiData.maxPrice.Value);
                }

                var suggestions = filteredProperties.Take(3).Select(p => new PropertyDto(
                    p.Id,
                    p.Title,
                    p.Description ?? "",
                    p.Price,
                    p.Address,
                    p.CreatedAt,
                    p.Tags ?? ""
                )).ToList();

                // אם לא נמצאו דירות, נעדכן את הניסוח בצורה ידידותית
                var finalReply = aiData.reply ?? "הנה הדירות שמצאתי עבורך:";
                if (suggestions.Count == 0 && !string.IsNullOrEmpty(aiData.city))
                {
                    finalReply = $"חיפשתי במאגר שלנו, אך כרגע לא נמצאו דירות תואמות ב{aiData.city} עד תקציב של {aiData.maxPrice} ש\"ח.";
                }

                return new ChatReply(finalReply, suggestions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chat Error: {ex.Message}");
                return GetFallbackReply("אירעה שגיאה פנימית במהלך שליפת הדירות.");
            }
        }
        private ChatReply GetFallbackReply(string message)
        {
            return new ChatReply(message, new List<PropertyDto>());
        }

        // מחלץ אובייקט JSON מתוך תשובת המודל, גם אם הוא עטוף ב-```json``` או מלווה בטקסט.
        private static OpenRouterChatResponse? TryParseAiResponse(string rawContent)
        {
            var start = rawContent.IndexOf('{');
            var end = rawContent.LastIndexOf('}');
            if (start < 0 || end <= start)
            {
                return null;
            }

            var json = rawContent.Substring(start, end - start + 1);
            try
            {
                return JsonSerializer.Deserialize<OpenRouterChatResponse>(
                    json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}