namespace NZWalks.API.Models.Domain.Inventory
{
    public enum SalesOrderStatus { Draft, Confirmed, PartiallyShipped, Shipped, Cancelled }

    public class SalesOrder
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public Guid CustomerId { get; set; }
        public Guid WarehouseId { get; set; }
        public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Draft;
        public DateTime OrderDate { get; set; }
        public DateTime? RequiredDate { get; set; }
        public DateTime? ShippedDate { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? ShippingAddress { get; set; }
        public string? Notes { get; set; }
        public Guid CreatedById { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Customer Customer { get; set; } = null!;
        public Warehouse Warehouse { get; set; } = null!;
        public AppUser CreatedBy { get; set; } = null!;
        public ICollection<SalesOrderItem> Items { get; set; } = new List<SalesOrderItem>();
    }
}