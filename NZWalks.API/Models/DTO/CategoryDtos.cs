using System.ComponentModel.DataAnnotations;

namespace NZWalks.API.Models.DTO.Category
{
    // ──────────────────────────────────────────────
    // REQUEST DTOs
    // ──────────────────────────────────────────────

    public class CreateCategoryRequestDto
    {
        [Required]
        [MaxLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public Guid? ParentCategoryId { get; set; }
    }

    public class UpdateCategoryRequestDto
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public Guid? ParentCategoryId { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // ──────────────────────────────────────────────
    // RESPONSE DTOs
    // ──────────────────────────────────────────────

    public class CategoryResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? ParentCategoryId { get; set; }
        public string? ParentCategoryName { get; set; }
        public int SubCategoryCount { get; set; }
        public bool HasSubCategories => SubCategoryCount > 0;
        public int ProductCount { get; set; }
        public bool IsActive { get; set; }
    }
}
