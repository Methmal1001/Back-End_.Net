using System.Data;

namespace NZWalks.API.Models.Domain.Inventory
{
    public class AppUser
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public Guid RoleId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Role Role { get; set; } = null!;
    }
}