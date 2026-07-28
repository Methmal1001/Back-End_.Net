using System.ComponentModel.DataAnnotations;

namespace NZWalks.API.Models.DTO.Restaurant
{
    public class CreateOrderRequestDto
    {
        [Required]
        public Guid TableId { get; set; }
    }

    public class AddOrderItemRequestDto
    {
        [Required]
        public Guid MenuItemId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        [MaxLength(500)]
        public string? SpecialInstructions { get; set; }

        public List<Guid>? SelectedModifierOptionIds { get; set; }
    }

    public class UpdateOrderItemRequestDto
    {
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        [MaxLength(500)]
        public string? SpecialInstructions { get; set; }

        public List<Guid>? SelectedModifierOptionIds { get; set; }
    }

    public class CancelOrderRequestDto
    {
        [MaxLength(500)]
        public string? Reason { get; set; }
    }

    public class OrderItemModifierResponseDto
    {
        public Guid Id { get; set; }
        public Guid ModifierOptionId { get; set; }
        public string ModifierGroupName { get; set; } = string.Empty;
        public string ModifierOptionName { get; set; } = string.Empty;
        public decimal PriceDelta { get; set; }
    }

    public class OrderItemResponseDto
    {
        public Guid Id { get; set; }
        public Guid MenuItemId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public string? SpecialInstructions { get; set; }
        public string Status { get; set; } = string.Empty;
        public string KitchenStation { get; set; } = string.Empty;
        public List<OrderItemModifierResponseDto> Modifiers { get; set; } = new();
    }

    public class OrderResponseDto
    {
        public Guid Id { get; set; }
        public Guid TableId { get; set; }
        public string TableName { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ServiceChargeAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public string? CreatedByUserName { get; set; }
        public string? LastUpdatedByUserName { get; set; }
        public List<OrderItemResponseDto> Items { get; set; } = new();
    }

    public class OrderSummaryResponseDto
    {
        public Guid Id { get; set; }
        public Guid TableId { get; set; }
        public string TableName { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
        public DateTime OpenedAt { get; set; }
    }

    public class OrderListResponseDto
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public List<OrderSummaryResponseDto> Orders { get; set; } = new();
    }
}
