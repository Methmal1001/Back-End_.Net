using System.ComponentModel.DataAnnotations;

namespace NZWalks.API.Models.Domain.Inventory
{
    public class ActivityLog
    {
        public Guid Id { get; set; }

        public Guid? UserId { get; set; }

        [MaxLength(256)]
        public string? UserName { get; set; }

        [MaxLength(100)]
        public string? UserRole { get; set; }

        [Required, MaxLength(100)]
        public string Module { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [Required, MaxLength(10)]
        public string HttpMethod { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string Path { get; set; } = string.Empty;

        public Guid? EntityId { get; set; }

        [Required, MaxLength(1000)]
        public string Summary { get; set; } = string.Empty;

        public int StatusCode { get; set; }

        public bool IsSuccess { get; set; }

        public string? Details { get; set; }

        [MaxLength(64)]
        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
