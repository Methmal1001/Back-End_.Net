namespace NZWalks.API.Models.Domain.Inventory
{
    public class Role
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}