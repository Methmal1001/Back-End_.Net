namespace NZWalks.API.Models.Domain.Restaurant
{
    // Scoped directly to a MenuItem (e.g. "Size", "Spice Level") rather than a
    // reusable many-to-many group — simplest fit for v1 menu configuration.
    public class ModifierGroup
    {
        public Guid Id { get; set; }
        public Guid MenuItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public int MinSelect { get; set; }
        public int MaxSelect { get; set; }

        public MenuItem MenuItem { get; set; } = null!;
        public ICollection<ModifierOption> Options { get; set; } = new List<ModifierOption>();
    }
}
