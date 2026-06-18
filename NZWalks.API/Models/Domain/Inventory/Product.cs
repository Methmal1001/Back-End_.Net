namespace NZWalks.API.Models.Domain.Inventory
{
    public class Product
    {
        public Guid Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid CategoryId { get; set; }
        public Guid? SupplierId { get; set; }
        public decimal UnitCost { get; set; }
        public decimal UnitPrice { get; set; }
        public string UnitOfMeasure { get; set; } = "Each";
        public int ReorderPoint { get; set; }
        public int ReorderQuantity { get; set; }
        public int MinStockLevel { get; set; }
        public int MaxStockLevel { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Barcode { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Guid? CreatedByUserId { get; set; }
        public string? CreatedByUserName { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public string? UpdatedByUserName { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public Category Category { get; set; } = null!;
        public Supplier? Supplier { get; set; }
        public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    }
}