namespace NZWalks.API.Models.Domain.Inventory
{
    public class PurchaseOrderItem
    {
        public Guid Id { get; set; }
        public Guid PurchaseOrderId { get; set; }
        public Guid ProductId { get; set; }
        public int QuantityOrdered { get; set; }
        public int QuantityReceived { get; set; }
        public decimal UnitCost { get; set; }
        public decimal LineTotal { get; set; }

        public PurchaseOrder PurchaseOrder { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}