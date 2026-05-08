namespace NZWalks.API.Models.Domain.Inventory
{
    public class StockTransferItem
    {
        public Guid Id { get; set; }
        public Guid TransferId { get; set; }
        public Guid ProductId { get; set; }
        public Guid FromLocationId { get; set; }
        public Guid ToLocationId { get; set; }
        public int QuantityRequested { get; set; }
        public int QuantityTransferred { get; set; }

        public StockTransfer Transfer { get; set; } = null!;
        public Product Product { get; set; } = null!;
        public StockLocation FromLocation { get; set; } = null!;
        public StockLocation ToLocation { get; set; } = null!;
    }
}