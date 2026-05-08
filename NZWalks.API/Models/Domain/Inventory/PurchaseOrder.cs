namespace NZWalks.API.Models.Domain.Inventory
{
    public enum PurchaseOrderStatus { Draft, Submitted, Approved, PartiallyReceived, Received, Cancelled }

    public class PurchaseOrder
    {
        public Guid Id { get; set; }
        public string PoNumber { get; set; } = string.Empty;
        public Guid SupplierId { get; set; }
        public Guid WarehouseId { get; set; }
        public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
        public DateTime OrderDate { get; set; }
        public DateTime? ExpectedDate { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }
        public Guid CreatedById { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Supplier Supplier { get; set; } = null!;
        public Warehouse Warehouse { get; set; } = null!;
        public AppUser CreatedBy { get; set; } = null!;
        public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
        public ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();
    }
}