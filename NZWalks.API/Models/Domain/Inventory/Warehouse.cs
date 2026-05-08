namespace NZWalks.API.Models.Domain.Inventory
{
    public class Warehouse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? ManagerName { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<WarehouseZone> Zones { get; set; } = new List<WarehouseZone>();
    }
}