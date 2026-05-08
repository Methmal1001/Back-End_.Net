namespace NZWalks.API.Models.Domain.Inventory
{
    public class StockCountItem
    {
        public Guid Id { get; set; }
        public Guid StockCountId { get; set; }
        public Guid ProductId { get; set; }
        public Guid LocationId { get; set; }
        public int SystemQuantity { get; set; }
        public int CountedQuantity { get; set; }
        public int Variance { get; set; }
        public Guid? CountedById { get; set; }

        public StockCount StockCount { get; set; } = null!;
        public Product Product { get; set; } = null!;
        public StockLocation Location { get; set; } = null!;
        public AppUser? CountedBy { get; set; }
    }
}