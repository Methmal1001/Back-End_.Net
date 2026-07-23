using NZWalks.API.Data;
using NZWalks.API.Models.Domain.WhatsApp;

namespace NZWalks.API.Repositories.WhatsApp
{
    public class WhatsAppConversationRepository : IWhatsAppConversationRepository
    {
        private readonly HrDbContext _db;
        public WhatsAppConversationRepository(HrDbContext db) => _db = db;

        public Task<WhatsAppConversation?> GetByPhoneNumberAsync(string phoneNumber)
        {
            // TODO: query _db.WhatsAppConversations by PhoneNumber.
            throw new NotImplementedException();
        }

        public Task<WhatsAppConversation> CreateAsync(WhatsAppConversation conversation)
        {
            // TODO: add to _db.WhatsAppConversations and save.
            throw new NotImplementedException();
        }

        public Task<WhatsAppConversation?> UpdateHistoryAsync(Guid id, string historyJson)
        {
            // TODO: load by id, update HistoryJson + LastMessageAt, save.
            throw new NotImplementedException();
        }
    }
}
