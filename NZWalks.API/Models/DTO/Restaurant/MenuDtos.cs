using System.ComponentModel.DataAnnotations;

namespace NZWalks.API.Models.DTO.Restaurant
{
    // ──────────────────────────────────────────────
    // CATEGORY DTOs
    // ──────────────────────────────────────────────

    public class CreateMenuCategoryRequestDto
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; }
    }

    public class UpdateMenuCategoryRequestDto
    {
        public Guid Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class MenuCategoryResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public int ItemCount { get; set; }
    }

    // ──────────────────────────────────────────────
    // MODIFIER DTOs (nested under menu item requests/responses)
    // ──────────────────────────────────────────────

    public class ModifierOptionRequestDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public decimal PriceDelta { get; set; }
    }

    public class ModifierGroupRequestDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsRequired { get; set; }
        public int MinSelect { get; set; }
        public int MaxSelect { get; set; } = 1;

        public List<ModifierOptionRequestDto> Options { get; set; } = new();
    }

    public class ModifierOptionResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal PriceDelta { get; set; }
    }

    public class ModifierGroupResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public int MinSelect { get; set; }
        public int MaxSelect { get; set; }
        public List<ModifierOptionResponseDto> Options { get; set; } = new();
    }

    // ──────────────────────────────────────────────
    // MENU ITEM DTOs
    // ──────────────────────────────────────────────

    public class CreateMenuItemRequestDto
    {
        [Required]
        public Guid MenuCategoryId { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public string? ImageUrl { get; set; }

        [Range(0, int.MaxValue)]
        public int PrepTimeMinutes { get; set; }

        [Required]
        public string KitchenStation { get; set; } = string.Empty;

        public List<ModifierGroupRequestDto>? ModifierGroups { get; set; }
    }

    public class UpdateMenuItemRequestDto
    {
        public Guid Id { get; set; }

        [Required]
        public Guid MenuCategoryId { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public string? ImageUrl { get; set; }

        [Range(0, int.MaxValue)]
        public int PrepTimeMinutes { get; set; }

        [Required]
        public string KitchenStation { get; set; } = string.Empty;

        public bool IsAvailable { get; set; } = true;
        public bool IsActive { get; set; } = true;

        // Replaces the item's modifier groups wholesale when supplied.
        public List<ModifierGroupRequestDto>? ModifierGroups { get; set; }
    }

    public class UpdateMenuItemAvailabilityRequestDto
    {
        public bool IsAvailable { get; set; }
    }

    public class MenuItemResponseDto
    {
        public Guid Id { get; set; }
        public Guid MenuCategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public int PrepTimeMinutes { get; set; }
        public string KitchenStation { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public bool IsActive { get; set; }
        public List<ModifierGroupResponseDto> ModifierGroups { get; set; } = new();
    }

    public class MenuItemListResponseDto
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public List<MenuItemResponseDto> Items { get; set; } = new();
    }
}
