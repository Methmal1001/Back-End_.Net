using System.Security.Cryptography.X509Certificates;

namespace NZWalks.API.Models.Domain.Inventory
{
    public enum ZoneType { Receiving, Storage, Picking, Shipping, Returns, Quarantine }

    public class WarehouseZone
    {
        public Guid Id { get; set; }
        public Guid WarehouseId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public ZoneType ZoneType { get; set; }

        public Warehouse Warehouse { get; set; } = null!;
        public ICollection<StockLocation> Locations { get; set; } = new List<StockLocation>();
    }
}