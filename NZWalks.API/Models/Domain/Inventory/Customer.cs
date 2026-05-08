namespace NZWalks.API.Models.Domain.Inventory
{
    public enum CustomerStatus { Active, Inactive, Suspended }

    public class Customer
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ContactName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Country { get; set; }
        public string? TaxId { get; set; }
        public decimal CreditLimit { get; set; }
        public CustomerStatus Status { get; set; } = CustomerStatus.Active;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
    }
}