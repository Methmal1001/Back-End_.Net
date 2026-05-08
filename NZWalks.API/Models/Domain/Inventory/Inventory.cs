namespace NZWalks.API.Models.Domain.Inventory
{
    public class Inventory
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public Guid LocationId { get; set; }
        public int QuantityOnHand { get; set; }
        public int QuantityReserved { get; set; }
        public int QuantityOnOrder { get; set; }
        public DateTime? LastCountedAt { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Product Product { get; set; } = null!;
        public StockLocation Location { get; set; } = null!;
    }
}