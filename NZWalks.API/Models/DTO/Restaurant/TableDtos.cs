using System.ComponentModel.DataAnnotations;

namespace NZWalks.API.Models.DTO.Restaurant
{
    public class CreateTableRequestDto
    {
        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Section { get; set; }

        [Range(1, int.MaxValue)]
        public int Capacity { get; set; }
    }

    public class UpdateTableRequestDto
    {
        public Guid Id { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Section { get; set; }

        [Range(1, int.MaxValue)]
        public int Capacity { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;
    }

    public class UpdateTableStatusRequestDto
    {
        [Required]
        public string Status { get; set; } = string.Empty;
    }

    public class TableResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Section { get; set; }
        public int Capacity { get; set; }
        public string Status { get; set; } = string.Empty;
        public Guid? OpenOrderId { get; set; }
    }
}
