using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using RealEstate.Application.Chat;

namespace RealEstate.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ChatController(IMediator mediator) => _mediator = mediator;

        [HttpPost]
        public async Task<ActionResult<ChatReply>> Send([FromBody] ChatRequestDto request)
        {
            // בדיקת בטיחות קטנה - אם הקליינט לא שלח מזהה שיחה, נייצר אחד זמני או קבוע
            // (בשלב הבא תוכלי להעביר מזהה אמיתי מהפרונטאנד)
            var conversationId = string.IsNullOrEmpty(request.ConversationId)
                ? "default_user_session"
                : request.ConversationId;

            // בניית ה-ChatQuery המעודכן עם שני הפרמטרים
            var query = new ChatQuery(request.Message, conversationId);

            // שליחה ל-Handler
            var result = await _mediator.Send(query);
            return Ok(result);
        }
    }

    // אובייקט עזר לקבלת המידע מהקליינט בשילוב עם מזהה השיחה
    public class ChatRequestDto
    {
        public string Message { get; set; } = string.Empty;
        public string? ConversationId { get; set; }
    }
}