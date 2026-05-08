namespace NZWalks.API.Models.Domain.Inventory
{
    public class StockAdjustmentItem
    {
        public Guid Id { get; set; }
        public Guid AdjustmentId { get; set; }
        public Guid ProductId { get; set; }
        public Guid LocationId { get; set; }
        public int QuantityBefore { get; set; }
        public int QuantityAfter { get; set; }
        public int QuantityChange { get; set; }

        public StockAdjustment Adjustment { get; set; } = null!;
        public Product Product { get; set; } = null!;
        public StockLocation Location { get; set; } = null!;
    }
}