namespace NZWalks.API.Models.Domain.Inventory
{
    public enum CountType { Full, Cycle, Spot }
    public enum CountStatus { Scheduled, InProgress, Completed, Cancelled }

    public class StockCount
    {
        public Guid Id { get; set; }
        public string CountNumber { get; set; } = string.Empty;
        public Guid WarehouseId { get; set; }
        public CountType CountType { get; set; }
        public CountStatus Status { get; set; } = CountStatus.Scheduled;
        public DateTime ScheduledDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public Guid CreatedById { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Warehouse Warehouse { get; set; } = null!;
        public AppUser CreatedBy { get; set; } = null!;
        public ICollection<StockCountItem> Items { get; set; } = new List<StockCountItem>();
    }
}