using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NZWalks.API.Models.DTO.Chatbot;
using NZWalks.API.Services;

namespace NZWalks.API.Controllers
{
    [Route("api/chatbot")]
    [ApiController]
    [Authorize]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _chatbot;

        public ChatbotController(IChatbotService chatbot)
        {
            _chatbot = chatbot;
        }

        // POST api/chatbot/ask
        // Body: { "message": "...", "history": [{ "role": "user"|"model", "content": "..." }] }
        // Returns: JSON { data.reply } for text answers, or text/csv file for export requests.
        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new CommonApiResponse<object>
                {
                    StatusCode = 400, IsSuccess = false,
                    Message = "Message cannot be empty.", Data = null
                });

            var result = await _chatbot.AskAsync(request, HttpContext.User);

            if (result.IsFile && result.FileBase64 != null)
            {
                var bytes = Convert.FromBase64String(result.FileBase64);
                return File(bytes, result.ContentType ?? "application/octet-stream", result.FileName ?? "export.csv");
            }

            return Ok(new CommonApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = "Chatbot response.",
                Data = new { reply = result.Reply }
            });
        }
    }
}
