namespace NZWalks.API.Models.Domain.Inventory
{
    public enum AuditAction { Create, Update, Delete }

    public class AuditLog
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public AuditAction Action { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public AppUser User { get; set; } = null!;
    }
}