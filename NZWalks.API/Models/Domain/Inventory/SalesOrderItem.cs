namespace NZWalks.API.Models.Domain.Inventory
{
    public class SalesOrderItem
    {
        public Guid Id { get; set; }
        public Guid SalesOrderId { get; set; }
        public Guid ProductId { get; set; }
        public int QuantityOrdered { get; set; }
        public int QuantityAllocated { get; set; }
        public int QuantityShipped { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal LineTotal { get; set; }

        public SalesOrder SalesOrder { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}