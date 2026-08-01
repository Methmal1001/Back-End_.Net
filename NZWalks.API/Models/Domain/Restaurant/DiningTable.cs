namespace NZWalks.API.Models.Domain.Restaurant
{
    public enum TableStatus { Available, Occupied, Reserved, Cleaning }

    public class DiningTable
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Section { get; set; }
        public int Capacity { get; set; }
        public TableStatus Status { get; set; } = TableStatus.Available;

        public Guid? CreatedByUserId { get; set; }
        public string? CreatedByUserName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid? LastUpdatedByUserId { get; set; }
        public string? LastUpdatedByUserName { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
    }
}
