namespace NZWalks.API.Models.Domain.Inventory
{
    public enum MovementType { Receipt, Sale, Transfer, Adjustment, Return, Count }

    public class StockMovement
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public Guid? FromLocationId { get; set; }
        public Guid? ToLocationId { get; set; }
        public int Quantity { get; set; }
        public MovementType MovementType { get; set; }
        public Guid? ReferenceId { get; set; }
        public string? ReferenceType { get; set; }
        public string? Notes { get; set; }
        public Guid CreatedById { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Product Product { get; set; } = null!;
        public AppUser CreatedBy { get; set; } = null!;
    }
}