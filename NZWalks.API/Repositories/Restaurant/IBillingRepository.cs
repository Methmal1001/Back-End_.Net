using NZWalks.API.Models.Domain.Restaurant;

namespace NZWalks.API.Repositories.Restaurant
{
    public class BillComputation
    {
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ServiceChargeAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public interface IBillingRepository
    {
        Task<Order?> GetOrderForBillingAsync(Guid orderId);
        BillComputation ComputeBill(Order order, decimal discountAmount);

        Task<(Order? order, Payment? payment, string? error)> ProcessPaymentAsync(
            Guid orderId, decimal amount, PaymentMethod method, decimal discountAmount, Guid? userId, string? userName);
    }
}
