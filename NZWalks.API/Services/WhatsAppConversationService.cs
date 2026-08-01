using NZWalks.API.Repositories.WhatsApp;

namespace NZWalks.API.Services
{
    public class WhatsAppConversationService : IWhatsAppConversationService
    {
        private readonly IWhatsAppConversationRepository _conversationRepo;
        private readonly IChatbotService _chatbotService;
        private readonly IWhatsAppMessagingService _messagingService;

        public WhatsAppConversationService(
            IWhatsAppConversationRepository conversationRepo,
            IChatbotService chatbotService,
            IWhatsAppMessagingService messagingService)
        {
            _conversationRepo = conversationRepo;
            _chatbotService = chatbotService;
            _messagingService = messagingService;
        }

        public Task HandleIncomingMessageAsync(string phoneNumber, string messageText)
        {
            // TODO (later steps):
            // 1. Resolve phoneNumber -> Employee -> AppUser.
            // 2. Load or create the persisted WhatsAppConversation via
            //    _conversationRepo.GetByPhoneNumberAsync/CreateAsync, and deserialize
            //    HistoryJson into List<ChatHistoryEntry>.
            // 3. Build a ChatRequest (Message = messageText, History = deserialized
            //    entries) and a ClaimsPrincipal carrying the resolved user's
            //    roleName/permission claims — mirroring TokenService's claim shape —
            //    since IChatbotService.AskAsync expects an authenticated-style principal.
            // 4. Call _chatbotService.AskAsync(request, principal).
            // 5. Append the exchange to history and persist via
            //    _conversationRepo.UpdateHistoryAsync.
            // 6. Send the reply via _messagingService.SendTextMessageAsync.
            throw new NotImplementedException();
        }
    }
}
