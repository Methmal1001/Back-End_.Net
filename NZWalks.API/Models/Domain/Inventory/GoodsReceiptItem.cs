namespace NZWalks.API.Models.Domain.Inventory
{
    public class GoodsReceiptItem
    {
        public Guid Id { get; set; }
        public Guid GoodsReceiptId { get; set; }
        public Guid PoItemId { get; set; }
        public Guid ProductId { get; set; }
        public int QuantityReceived { get; set; }
        public int QuantityAccepted { get; set; }
        public int QuantityRejected { get; set; }
        public string? RejectionReason { get; set; }
        public Guid LocationId { get; set; }

        public GoodsReceipt GoodsReceipt { get; set; } = null!;
        public PurchaseOrderItem PoItem { get; set; } = null!;
        public Product Product { get; set; } = null!;
        public StockLocation Location { get; set; } = null!;
    }
}