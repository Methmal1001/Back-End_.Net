namespace NZWalks.API.Models.Domain.Inventory
{
    public enum GoodsReceiptStatus { Pending, Partial, Complete }

    public class GoodsReceipt
    {
        public Guid Id { get; set; }
        public Guid PurchaseOrderId { get; set; }
        public Guid WarehouseId { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public DateTime ReceiptDate { get; set; }
        public GoodsReceiptStatus Status { get; set; }
        public string? Notes { get; set; }
        public Guid ReceivedById { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public PurchaseOrder PurchaseOrder { get; set; } = null!;
        public Warehouse Warehouse { get; set; } = null!;
        public AppUser ReceivedBy { get; set; } = null!;
        public ICollection<GoodsReceiptItem> Items { get; set; } = new List<GoodsReceiptItem>();
    }
}