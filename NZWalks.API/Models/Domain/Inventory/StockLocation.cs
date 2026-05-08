namespace NZWalks.API.Models.Domain.Inventory
{
    public class StockLocation
    {
        public Guid Id { get; set; }
        public Guid ZoneId { get; set; }
        public string? Aisle { get; set; }
        public string? Rack { get; set; }
        public string? Shelf { get; set; }
        public string? Bin { get; set; }
        public int Capacity { get; set; }

        public WarehouseZone Zone { get; set; } = null!;
        public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    }
}