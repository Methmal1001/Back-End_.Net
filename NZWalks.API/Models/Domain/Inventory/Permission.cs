namespace NZWalks.API.Models.Domain.Inventory
{
    public class Permission
    {
        public Guid Id { get; set; }
        public string Module { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Description { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}