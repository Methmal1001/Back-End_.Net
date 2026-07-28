namespace NZWalks.API.Models.Domain.Restaurant
{
    public enum KitchenStation { Grill, Bar, Dessert, Cold, Fryer }

    public class MenuItem
    {
        public Guid Id { get; set; }
        public Guid MenuCategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public int PrepTimeMinutes { get; set; }
        public KitchenStation KitchenStation { get; set; }

        // "86'd" toggle — temporary out-of-stock, independent of IsActive (permanent removal)
        public bool IsAvailable { get; set; } = true;
        public bool IsActive { get; set; } = true;

        public Guid? CreatedByUserId { get; set; }
        public string? CreatedByUserName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid? LastUpdatedByUserId { get; set; }
        public string? LastUpdatedByUserName { get; set; }
        public DateTime? LastUpdatedAt { get; set; }

        public MenuCategory Category { get; set; } = null!;
        public ICollection<ModifierGroup> ModifierGroups { get; set; } = new List<ModifierGroup>();
    }
}
