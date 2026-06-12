namespace NZWalks.API.Models.Domain.Inventory
{
    public enum ProductAuditAction { Create, Update, Delete }

    public class ProductAuditLog
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public ProductAuditAction Action { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Details { get; set; }
    }
}
