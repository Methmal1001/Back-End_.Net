namespace NZWalks.API.Services
{
    // Orchestrates an incoming WhatsApp message end-to-end: resolves the sender,
    // loads/saves conversation history, delegates to the existing chatbot, and
    // sends the reply back out over WhatsApp.
    public interface IWhatsAppConversationService
    {
        Task HandleIncomingMessageAsync(string phoneNumber, string messageText);
    }
}
