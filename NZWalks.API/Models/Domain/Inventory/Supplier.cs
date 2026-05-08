namespace NZWalks.API.Models.Domain.Inventory
{
    public enum SupplierStatus { Active, Inactive, Blacklisted }

    public class Supplier
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ContactName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Country { get; set; }
        public SupplierStatus Status { get; set; } = SupplierStatus.Active;
        public decimal? Rating { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    }
}