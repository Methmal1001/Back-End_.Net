namespace NZWalks.API.Models.Domain.WhatsApp
{
    public class WhatsAppConversation
    {
        public Guid Id { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public Guid? EmployeeId { get; set; }

        // Serialized List<ChatHistoryEntry> (see Models.DTO.Chatbot) — kept as a
        // plain string here so this entity has no dependency on the chatbot DTOs.
        public string HistoryJson { get; set; } = "[]";

        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
