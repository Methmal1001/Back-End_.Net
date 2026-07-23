using Microsoft.AspNetCore.Mvc;
using NZWalks.API.Models.DTO.WhatsApp;
using NZWalks.API.Services;

namespace NZWalks.API.Controllers
{
    // Meta calls this endpoint unauthenticated — no [Authorize] here.
    // Webhook verify-token / X-Hub-Signature validation is added in a later step.
    [Route("api/whatsapp")]
    [ApiController]
    public class WhatsAppController : ControllerBase
    {
        private readonly IWhatsAppConversationService _conversationService;

        public WhatsAppController(IWhatsAppConversationService conversationService)
        {
            _conversationService = conversationService;
        }

        // GET api/whatsapp — Meta's webhook verification handshake.
        [HttpGet]
        public IActionResult VerifyWebhook(
            [FromQuery(Name = "hub.mode")] string? hubMode,
            [FromQuery(Name = "hub.verify_token")] string? hubVerifyToken,
            [FromQuery(Name = "hub.challenge")] string? hubChallenge)
        {
            // TODO: compare hubVerifyToken against WhatsAppSettings.VerifyToken and,
            // if it matches, return hubChallenge as plain text (200 OK); otherwise 403.
            throw new NotImplementedException();
        }

        // POST api/whatsapp — incoming message webhook.
        [HttpPost]
        public IActionResult ReceiveMessage([FromBody] WhatsAppWebhookPayload payload)
        {
            // TODO: walk payload.Entry -> Changes -> Value -> Messages, extract the
            // sender's phone number + message text for each message, and call
            // _conversationService.HandleIncomingMessageAsync(...) for each one.
            // Must always return 200 quickly regardless of downstream outcome —
            // Meta retries/disables the webhook on non-2xx or slow responses.
            throw new NotImplementedException();
        }
    }
}
