namespace NZWalks.API.Models.Domain.Restaurant
{
    // Snapshot of a selected ModifierOption at order time — name/price are copied
    // so later menu edits never change the price of an already-placed order.
    public class OrderItemModifier
    {
        public Guid Id { get; set; }
        public Guid OrderItemId { get; set; }
        public Guid ModifierOptionId { get; set; }
        public string ModifierGroupName { get; set; } = string.Empty;
        public string ModifierOptionName { get; set; } = string.Empty;
        public decimal PriceDelta { get; set; }

        public OrderItem OrderItem { get; set; } = null!;
    }
}
