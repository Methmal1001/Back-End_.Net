using System.ComponentModel.DataAnnotations;

namespace NZWalks.API.Models.DTO.Restaurant
{
    public class BillLineItemDto
    {
        public string MenuItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class BillResponseDto
    {
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public List<BillLineItemDto> Items { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ServiceChargeAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ProcessPaymentRequestDto
    {
        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal DiscountAmount { get; set; }
    }

    public class PaymentResponseDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime PaidAt { get; set; }
    }

    public class PaymentResultResponseDto
    {
        public PaymentResponseDto Payment { get; set; } = null!;
        public BillResponseDto Bill { get; set; } = null!;
        public string OrderStatus { get; set; } = string.Empty;
    }
}
