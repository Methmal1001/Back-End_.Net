namespace NZWalks.API.Models.Domain.Inventory
{
    public enum AdjustmentReason { Damaged, Expired, Lost, Found, CycleCount, Other }
    public enum AdjustmentStatus { Draft, PendingApproval, Approved, Rejected }

    public class StockAdjustment
    {
        public Guid Id { get; set; }
        public string AdjustmentNumber { get; set; } = string.Empty;
        public Guid WarehouseId { get; set; }
        public AdjustmentReason Reason { get; set; }
        public string? Notes { get; set; }
        public AdjustmentStatus Status { get; set; } = AdjustmentStatus.Draft;
        public Guid? ApprovedById { get; set; }
        public Guid CreatedById { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Warehouse Warehouse { get; set; } = null!;
        public AppUser? ApprovedBy { get; set; }
        public AppUser CreatedBy { get; set; } = null!;
        public ICollection<StockAdjustmentItem> Items { get; set; } = new List<StockAdjustmentItem>();
    }
}