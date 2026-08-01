namespace NZWalks.API.Models.Domain.Restaurant
{
    public enum OrderStatus { Open, SentToKitchen, InProgress, ReadyToServe, Served, Billed, Closed, Cancelled }

    public class Order
    {
        public Guid Id { get; set; }
        public Guid TableId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public OrderStatus Status { get; set; } = OrderStatus.Open;

        // Computed/persisted at billing time — zero until then.
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ServiceChargeAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }

        public Guid? CreatedByUserId { get; set; }
        public string? CreatedByUserName { get; set; }
        public Guid? LastUpdatedByUserId { get; set; }
        public string? LastUpdatedByUserName { get; set; }
        public DateTime? LastUpdatedAt { get; set; }

        public DiningTable Table { get; set; } = null!;
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
