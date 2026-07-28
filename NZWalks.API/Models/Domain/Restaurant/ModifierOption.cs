namespace NZWalks.API.Models.Domain.Restaurant
{
    public class ModifierOption
    {
        public Guid Id { get; set; }
        public Guid ModifierGroupId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal PriceDelta { get; set; }

        public ModifierGroup ModifierGroup { get; set; } = null!;
    }
}
