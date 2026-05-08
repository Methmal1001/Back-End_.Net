namespace NZWalks.API.Models.Domain.Inventory
{
    public enum TransferStatus { Draft, InTransit, Completed, Cancelled }

    public class StockTransfer
    {
        public Guid Id { get; set; }
        public string TransferNumber { get; set; } = string.Empty;
        public Guid FromWarehouseId { get; set; }
        public Guid ToWarehouseId { get; set; }
        public TransferStatus Status { get; set; } = TransferStatus.Draft;
        public DateTime TransferDate { get; set; }
        public string? Notes { get; set; }
        public Guid CreatedById { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Warehouse FromWarehouse { get; set; } = null!;
        public Warehouse ToWarehouse { get; set; } = null!;
        public AppUser CreatedBy { get; set; } = null!;
        public ICollection<StockTransferItem> Items { get; set; } = new List<StockTransferItem>();
    }
}