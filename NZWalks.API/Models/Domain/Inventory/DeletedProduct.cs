namespace NZWalks.API.Models.Domain.Inventory
{
    public class DeletedProduct
    {
        public Guid Id { get; set; }

        // Snapshot of the product at deletion time
        public Guid OriginalProductId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public Guid? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public decimal UnitCost { get; set; }
        public decimal UnitPrice { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public int ReorderPoint { get; set; }
        public int ReorderQuantity { get; set; }
        public int MinStockLevel { get; set; }
        public int MaxStockLevel { get; set; }
        public string? Barcode { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime OriginalCreatedAt { get; set; }

        // Deletion metadata
        public string DeletedByUserId { get; set; } = string.Empty;
        public string DeletedByUserName { get; set; } = string.Empty;
        public string? DeletionReason { get; set; }
        public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
    }
}