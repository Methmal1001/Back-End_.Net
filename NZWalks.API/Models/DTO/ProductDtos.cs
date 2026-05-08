using System.ComponentModel.DataAnnotations;

namespace NZWalks.API.Models.DTO.Product
{
    // ──────────────────────────────────────────────
    // REQUEST DTOs
    // ──────────────────────────────────────────────

    public class CreateProductRequestDto
    {
        [Required]
        [MaxLength(50, ErrorMessage = "SKU cannot exceed 50 characters.")]
        public string Sku { get; set; } = string.Empty;

        [Required]
        [MaxLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public Guid CategoryId { get; set; }

        public Guid? SupplierId { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Unit cost must be 0 or greater.")]
        public decimal UnitCost { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Unit price must be 0 or greater.")]
        public decimal UnitPrice { get; set; }

        [Required]
        [MaxLength(50)]
        public string UnitOfMeasure { get; set; } = "Each";

        [Range(0, int.MaxValue)]
        public int ReorderPoint { get; set; }

        [Range(0, int.MaxValue)]
        public int ReorderQuantity { get; set; }

        [Range(0, int.MaxValue)]
        public int MinStockLevel { get; set; }

        [Range(0, int.MaxValue)]
        public int MaxStockLevel { get; set; }

        [MaxLength(100)]
        public string? Barcode { get; set; }

        public string? ImageUrl { get; set; }
    }

    public class UpdateProductRequestDto
    {
        [Required]
        [MaxLength(50)]
        public string Sku { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public Guid CategoryId { get; set; }

        public Guid? SupplierId { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal UnitCost { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [Required]
        [MaxLength(50)]
        public string UnitOfMeasure { get; set; } = "Each";

        [Range(0, int.MaxValue)]
        public int ReorderPoint { get; set; }

        [Range(0, int.MaxValue)]
        public int ReorderQuantity { get; set; }

        [Range(0, int.MaxValue)]
        public int MinStockLevel { get; set; }

        [Range(0, int.MaxValue)]
        public int MaxStockLevel { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(100)]
        public string? Barcode { get; set; }

        public string? ImageUrl { get; set; }
    }

    public class DeleteProductRequestDto
    {
        [MaxLength(500)]
        public string? DeletionReason { get; set; }

        // In a real app this would come from the auth token.
        // For testing via Swagger we accept it in the body.
        [Required]
        public string DeletedByUserId { get; set; } = string.Empty;

        [Required]
        public string DeletedByUserName { get; set; } = string.Empty;
    }

    // ──────────────────────────────────────────────
    // RESPONSE DTOs
    // ──────────────────────────────────────────────

    public class ProductResponseDto
    {
        public Guid Id { get; set; }
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
        public bool IsActive { get; set; }
        public string? Barcode { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DeletedProductResponseDto
    {
        public Guid Id { get; set; }
        public Guid OriginalProductId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? SupplierName { get; set; }
        public decimal UnitCost { get; set; }
        public decimal UnitPrice { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public DateTime OriginalCreatedAt { get; set; }
        public string DeletedByUserId { get; set; } = string.Empty;
        public string DeletedByUserName { get; set; } = string.Empty;
        public string? DeletionReason { get; set; }
        public DateTime DeletedAt { get; set; }
    }

    public class ProductListResponseDto
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public List<ProductResponseDto> Products { get; set; } = new();
    }
}