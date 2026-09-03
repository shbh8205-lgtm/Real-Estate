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
    // תשובת ה-AI לשלב הבנת הכוונה (Query Understanding) - עדיין JSON קשיח,
    // אבל עכשיו משמש רק לחילוץ פילטרים + ניסוח שאילתת חיפוש סמנטית, לא ליצירת התשובה הסופית.
    public class QueryIntent
    {
        public string? searchQuery { get; set; }     // תיאור חופשי לחיפוש סמנטי (Embedding)
        public string? cityHebrew { get; set; }
        public string? cityEnglish { get; set; }
        public long? maxPrice { get; set; }
    }

    // תור שיחה מינימלי שנשמר במטמון בין קריאות, כדי לשמר הקשר רב-תורי
    // (למשל: "תראה לי עוד" אחרי ששאלת קודם על עיר מסוימת)
    public record ChatTurn(string Role, string Content);

    public class ChatHandler : IRequestHandler<ChatQuery, ChatReply>
    {
        private readonly HttpClient _httpClient;
        private readonly IAsyncRepository<Property> _propertyRepository;
        private readonly IAiPropertyAnalyst _aiAnalyst;
        private readonly IMemoryCache _cache;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _baseUrl;

        private const int SuggestionsCount = 3;

        public ChatHandler(
            HttpClient httpClient,
            IAsyncRepository<Property> propertyRepository,
            IAiPropertyAnalyst aiAnalyst,
            IConfiguration configuration,
            IMemoryCache cache)
        {
            _httpClient = httpClient;
            _propertyRepository = propertyRepository;
            _aiAnalyst = aiAnalyst;
            _cache = cache;
            _apiKey = configuration["OpenRouter:ApiKey"] ?? "";
            _model = configuration["OpenRouter:Model"] ?? "google/gemini-2.5-flash";
            _baseUrl = configuration["OpenRouter:BaseUrl"] ?? "https://openrouter.ai/api/v1";
        }

        public async Task<ChatReply> Handle(ChatQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"chat_turns_{request.ConversationId}";
            var turns = _cache.Get<List<ChatTurn>>(cacheKey) ?? new List<ChatTurn>();

            try
            {
                // ==== שלב 1: הבנת כוונה (Query Understanding) ====
                // קריאה קצרה למודל שרק מחלצת פילטרים קשיחים (עיר/תקציב) ומנסחת שאילתת
                // חיפוש סמנטית מתוך כל השיחה. זה עדיין לא ה-RAG עצמו - זו רק הכנת השאילתה.
                var intent = await ExtractIntentAsync(turns, request.Message, cancellationToken);
                var searchQuery = string.IsNullOrWhiteSpace(intent?.searchQuery) ? request.Message : intent!.searchQuery!;

                // ==== שלב 2: אחזור (Retrieval) - זהו ה-RAG האמיתי ====
                // מייצרים embedding לשאילתה ומדרגים את הנכסים לפי דמיון קוסינוס
                // מול ה-DescriptionVector שכבר מחושב ונשמר לכל נכס.
                var queryVector = await _aiAnalyst.GenerateEmbeddingAsync(searchQuery);
                var allProperties = await _propertyRepository.ListAllAsync();

                var cityCandidates = intent is null ? new List<string>() : BuildCityCandidates(intent);
                var ranked = RankProperties(allProperties, queryVector, cityCandidates, intent?.maxPrice);

                var suggestions = ranked.Take(SuggestionsCount)
                    .Select(p => new PropertyDto(
                        p.Id, p.Title, p.Description ?? "", p.Price, p.Address, p.CreatedAt, p.Tags ?? ""))
                    .ToList();

                // ==== שלב 3: יצירה מבוססת-הקשר (Augmented Generation) ====
                // נותנים למודל בדיוק את הנכסים שאחזרנו (ורק אותם) ומבקשים תשובה טבעית
                // המתבססת עליהם, כדי למנוע המצאת נכסים שלא קיימים במאגר.
                var finalReply = await GenerateGroundedReplyAsync(turns, request.Message, suggestions, cityCandidates, intent?.maxPrice, cancellationToken);

                turns.Add(new ChatTurn("user", request.Message));
                turns.Add(new ChatTurn("assistant", finalReply));
                if (turns.Count > 20) turns = turns.Skip(turns.Count - 20).ToList(); // מניעת גדילה בלתי מוגבלת
                _cache.Set(cacheKey, turns, TimeSpan.FromMinutes(20));

                return new ChatReply(finalReply, suggestions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chat Error: {ex.Message}");
                return GetFallbackReply("אירעה שגיאה פנימית במהלך שליפת הדירות.");
            }
        }

        // קריאה 1 מתוך 2 ל-LLM: חילוץ כוונה + שאילתת חיפוש סמנטית מתוך השיחה
        private async Task<QueryIntent?> ExtractIntentAsync(List<ChatTurn> turns, string userMessage, CancellationToken cancellationToken)
        {
            var systemPrompt = @"You are the query-understanding module of a real-estate RAG search engine.
Read the conversation and return ONLY a strict JSON object describing the user's CURRENT search intent:
{
    ""searchQuery"": ""A short free-text description (in Hebrew) of what the user is looking for - style, rooms, features, vibe. Used for semantic vector search, so make it descriptive, not just a city name."",
    ""cityHebrew"": ""City name in Hebrew if mentioned (in this message or earlier in the conversation), else empty string"",
    ""cityEnglish"": ""The SAME city name in English, else empty string"",
    ""maxPrice"": 3000000
}
Rules:
- If the user refers back to earlier context (e.g. 'show me more', 'what else is there'), reuse the city/price/intent from earlier turns.
- NEVER reply with plain text or explanations - JSON only.";

            var messages = new List<object> { new { role = "system", content = systemPrompt } };
            messages.AddRange(turns.Select(t => (object)new { role = t.Role, content = t.Content }));
            messages.Add(new { role = "user", content = userMessage });

            var rawContent = await CallChatCompletionAsync(messages, maxTokens: 300, cancellationToken);
            if (string.IsNullOrWhiteSpace(rawContent)) return null;

            return TryParseJson<QueryIntent>(rawContent);
        }

        // קריאה 2 מתוך 2 ל-LLM: ניסוח תשובה טבעית שמבוססת אך ורק על הנכסים שאוחזרו
        private async Task<string> GenerateGroundedReplyAsync(
            List<ChatTurn> turns,
            string userMessage,
            List<PropertyDto> retrievedProperties,
            List<string> cityCandidates,
            long? maxPrice,
            CancellationToken cancellationToken)
        {
            var systemPrompt = @"You are a warm, professional Hebrew-speaking real-estate assistant.
You will be given a list of RETRIEVED properties that were already matched to the user via semantic search -
this is the ONLY data you are allowed to mention. Write a short, encouraging reply in Hebrew (2-4 sentences):
- If the list is non-empty, briefly highlight why 1-3 of them fit (mention title/price/address naturally).
- If the list is empty, say so honestly and suggest the user broaden the city or budget.
- NEVER invent properties, prices, or addresses that are not in the retrieved list.
- Reply with plain Hebrew text only - no JSON, no markdown, no field names.";

            var contextPayload = new
            {
                userMessage,
                cityMentioned = cityCandidates.FirstOrDefault(),
                maxPriceMentioned = maxPrice,
                retrievedProperties = retrievedProperties.Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Description,
                    p.Price,
                    p.Address,
                    p.Tags
                })
            };

            var messages = new List<object> { new { role = "system", content = systemPrompt } };
            messages.AddRange(turns.TakeLast(6).Select(t => (object)new { role = t.Role, content = t.Content }));
            messages.Add(new { role = "user", content = JsonSerializer.Serialize(contextPayload) });

            var reply = await CallChatCompletionAsync(messages, maxTokens: 400, cancellationToken);

            if (!string.IsNullOrWhiteSpace(reply)) return reply.Trim();

            // רשת ביטחון אם קריאת ה-LLM נכשלה: תשובה בסיסית שעדיין נכונה ביחס לתוצאות שאוחזרו
            if (retrievedProperties.Count > 0)
            {
                return "הנה הדירות שמצאתי עבורך במאגר:";
            }

            var displayCity = cityCandidates.FirstOrDefault();
            return displayCity is null
                ? "לא הצלחתי למצוא דירות מתאימות במאגר כרגע. נסה לתאר מה אתה מחפש בפירוט רב יותר."
                : $"חיפשתי במאגר שלנו, אך כרגע לא נמצאו דירות תואמות ב{displayCity}" + (maxPrice.HasValue ? $" עד תקציב של {maxPrice} ש\"ח." : ".");
        }

        // קריאה גנרית ל-endpoint של chat/completions
        private async Task<string?> CallChatCompletionAsync(List<object> messages, int maxTokens, CancellationToken cancellationToken)
        {
            var requestBody = new
            {
                model = _model,
                max_tokens = maxTokens,
                messages
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions");
            httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");
            httpRequest.Content = JsonContent.Create(requestBody);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var jsonResult = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            return jsonResult.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        }

        // דירוג הנכסים: קודם פילטר קשיח (עיר/תקציב) אם קיים, ואז מיון לפי דמיון סמנטי
        // לשאילתה (Vector Search). אם הפילטר הקשיח לא החזיר תוצאות, נופלים חזרה לדירוג
        // סמנטי על פני כל המאגר, כדי שהמשתמש עדיין יקבל את ההתאמות הכי קרובות שיש.
        private static List<Property> RankProperties(
            IReadOnlyList<Property> allProperties,
            float[] queryVector,
            List<string> cityCandidates,
            long? maxPrice)
        {
            IEnumerable<Property> filtered = allProperties;

            if (cityCandidates.Count > 0)
            {
                filtered = filtered.Where(p =>
                    !string.IsNullOrEmpty(p.Address) &&
                    cityCandidates.Any(c => p.Address.Contains(c, StringComparison.OrdinalIgnoreCase)));
            }

            if (maxPrice.HasValue && maxPrice.Value > 0)
            {
                filtered = filtered.Where(p => p.Price <= maxPrice.Value);
            }

            var candidates = filtered.ToList();
            if (candidates.Count == 0)
            {
                // אין תוצאות עם הפילטר הקשיח - נופלים חזרה לכל המאגר ומדרגים סמנטית בלבד
                candidates = allProperties.ToList();
            }

            var hasQueryVector = VectorMath.IsUsable(queryVector);

            return candidates
                .Select(p => new
                {
                    Property = p,
                    Score = hasQueryVector ? VectorMath.CosineSimilarity(queryVector, p.DescriptionVector) : 0d
                })
                .OrderByDescending(x => x.Score)
                .Select(x => x.Property)
                .ToList();
        }

        private ChatReply GetFallbackReply(string message)
        {
            return new ChatReply(message, new List<PropertyDto>());
        }

        // קבוצות שמות נרדפים לערים נפוצות בישראל (עברית + אנגלית).
        // משמש כרשת ביטחון אם ה-AI החזיר רק שפה אחת.
        private static readonly string[][] CityAliasGroups = new[]
        {
            new[] { "תל אביב", "Tel Aviv" },
            new[] { "ירושלים", "Jerusalem" },
            new[] { "חיפה", "Haifa" },
            new[] { "באר שבע", "Beer Sheva", "Be'er Sheva" },
            new[] { "ראשון לציון", "Rishon LeZion", "Rishon Lezion" },
            new[] { "פתח תקווה", "Petah Tikva", "Petach Tikva" },
            new[] { "נתניה", "Netanya" },
            new[] { "אשדוד", "Ashdod" },
            new[] { "אשקלון", "Ashkelon" },
            new[] { "חולון", "Holon" },
            new[] { "רמת גן", "Ramat Gan" },
            new[] { "בני ברק", "Bnei Brak" },
            new[] { "הרצליה", "Herzliya", "Herzliyya" },
            new[] { "כפר סבא", "Kfar Saba" },
            new[] { "רעננה", "Raanana", "Ra'anana" },
            new[] { "בת ים", "Bat Yam" },
            new[] { "עפולה", "Afula" },
            new[] { "אילת", "Eilat" },
            new[] { "נצרת", "Nazareth" },
            new[] { "מודיעין", "Modiin", "Modi'in" },
            new[] { "רחובות", "Rehovot" },
            new[] { "אום אל פחם", "Umm al-Fahm" },
        };

        // בונה רשימת מחרוזות לחיפוש בכתובת: מה שה-AI החזיר (עברית/אנגלית)
        // בתוספת שמות נרדפים מוכרים, ללא כפילויות.
        private static List<string> BuildCityCandidates(QueryIntent intent)
        {
            var candidates = new List<string>();

            void AddUnique(string value)
            {
                var v = value.Trim();
                if (v.Length > 0 && !candidates.Any(x => string.Equals(x, v, StringComparison.OrdinalIgnoreCase)))
                {
                    candidates.Add(v);
                }
            }

            foreach (var raw in new[] { intent.cityHebrew, intent.cityEnglish })
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var c = raw.Trim();
                AddUnique(c);

                // אם העיר מוכרת, נוסיף את שמותיה בשפות האחרות
                foreach (var group in CityAliasGroups)
                {
                    var inGroup = group.Any(g =>
                        g.Contains(c, StringComparison.OrdinalIgnoreCase) ||
                        c.Contains(g, StringComparison.OrdinalIgnoreCase));
                    if (inGroup)
                    {
                        foreach (var alias in group) AddUnique(alias);
                    }
                }
            }

            return candidates;
        }

        // מחלץ אובייקט JSON מתוך תשובת המודל, גם אם הוא עטוף ב-```json``` או מלווה בטקסט.
        private static T? TryParseJson<T>(string rawContent) where T : class
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
                return JsonSerializer.Deserialize<T>(
                    json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
