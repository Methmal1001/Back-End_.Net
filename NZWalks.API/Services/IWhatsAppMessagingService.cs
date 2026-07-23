namespace NZWalks.API.Services
{
    // Outbound-only wrapper around the WhatsApp Cloud API "send message" endpoint.
    public interface IWhatsAppMessagingService
    {
        Task SendTextMessageAsync(string toPhoneNumber, string message);
    }
}
