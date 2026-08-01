namespace NZWalks.API.Models.Domain.Restaurant
{
    public enum PaymentMethod { Cash, Card, Other }

    public class Payment
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public DateTime PaidAt { get; set; } = DateTime.UtcNow;

        public Guid? CreatedByUserId { get; set; }
        public string? CreatedByUserName { get; set; }

        public Order Order { get; set; } = null!;
    }
}
