namespace NZWalks.API.Models.Domain.Restaurant
{
    public enum OrderItemStatus { Pending, Preparing, Ready, Served, Cancelled }

    public class OrderItem
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid MenuItemId { get; set; }
        public int Quantity { get; set; }
        public string? SpecialInstructions { get; set; }

        // Snapshot of MenuItem.Price + modifier deltas at order time.
        public decimal UnitPrice { get; set; }
        public OrderItemStatus Status { get; set; } = OrderItemStatus.Pending;

        // Snapshot from MenuItem so kitchen tickets can filter/group without a join.
        public KitchenStation KitchenStation { get; set; }

        public Order Order { get; set; } = null!;
        public MenuItem MenuItem { get; set; } = null!;
        public ICollection<OrderItemModifier> Modifiers { get; set; } = new List<OrderItemModifier>();
    }
}
