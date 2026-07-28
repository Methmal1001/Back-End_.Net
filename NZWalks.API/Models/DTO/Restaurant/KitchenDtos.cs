using System.ComponentModel.DataAnnotations;

namespace NZWalks.API.Models.DTO.Restaurant
{
    public class UpdateItemStatusRequestDto
    {
        [Required]
        public string Status { get; set; } = string.Empty;
    }

    public class KitchenTicketItemDto
    {
        public Guid OrderItemId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? SpecialInstructions { get; set; }
        public string Status { get; set; } = string.Empty;
        public string KitchenStation { get; set; } = string.Empty;
        public List<string> Modifiers { get; set; } = new();
    }

    public class KitchenTicketDto
    {
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public DateTime OpenedAt { get; set; }
        public List<KitchenTicketItemDto> Items { get; set; } = new();
    }
}
