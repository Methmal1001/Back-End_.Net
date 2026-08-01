using NZWalks.API.Models.Domain.WhatsApp;

namespace NZWalks.API.Repositories.WhatsApp
{
    public interface IWhatsAppConversationRepository
    {
        Task<WhatsAppConversation?> GetByPhoneNumberAsync(string phoneNumber);
        Task<WhatsAppConversation> CreateAsync(WhatsAppConversation conversation);
        Task<WhatsAppConversation?> UpdateHistoryAsync(Guid id, string historyJson);
    }
}
