using Microsoft.EntityFrameworkCore;
using NZWalks.API.Data;
using NZWalks.API.Models.Domain.Restaurant;

namespace NZWalks.API.Repositories.Restaurant
{
    public class BillingRepository : IBillingRepository
    {
        // v1 fixed rates — not yet configurable per venue/region.
        private const decimal TaxRatePercent = 10m;
        private const decimal ServiceChargeRatePercent = 5m;

        private readonly RestaurantDbContext _db;

        public BillingRepository(RestaurantDbContext db)
        {
            _db = db;
        }

        public async Task<Order?> GetOrderForBillingAsync(Guid orderId)
        {
            return await _db.Orders
                .Include(o => o.Table)
                .Include(o => o.Items).ThenInclude(i => i.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public BillComputation ComputeBill(Order order, decimal discountAmount)
        {
            var subtotal = order.Items
                .Where(i => i.Status != OrderItemStatus.Cancelled)
                .Sum(i => i.UnitPrice * i.Quantity);

            var tax = Math.Round(subtotal * TaxRatePercent / 100m, 2);
            var serviceCharge = Math.Round(subtotal * ServiceChargeRatePercent / 100m, 2);
            var total = Math.Max(0, subtotal + tax + serviceCharge - discountAmount);

            return new BillComputation
            {
                Subtotal = subtotal,
                TaxAmount = tax,
                ServiceChargeAmount = serviceCharge,
                DiscountAmount = discountAmount,
                TotalAmount = total
            };
        }

        public async Task<(Order? order, Payment? payment, string? error)> ProcessPaymentAsync(
            Guid orderId, decimal amount, PaymentMethod method, decimal discountAmount, Guid? userId, string? userName)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) return (null, null, $"Order with ID '{orderId}' was not found.");

            if (order.Status is OrderStatus.Billed or OrderStatus.Closed)
                return (null, null, "This order has already been billed.");

            if (order.Status == OrderStatus.Cancelled)
                return (null, null, "Cannot bill a cancelled order.");

            var bill = ComputeBill(order, discountAmount);

            order.Subtotal = bill.Subtotal;
            order.TaxAmount = bill.TaxAmount;
            order.ServiceChargeAmount = bill.ServiceChargeAmount;
            order.DiscountAmount = bill.DiscountAmount;
            order.TotalAmount = bill.TotalAmount;
            order.Status = OrderStatus.Closed;
            order.ClosedAt = DateTime.UtcNow;
            order.LastUpdatedByUserId = userId;
            order.LastUpdatedByUserName = userName;
            order.LastUpdatedAt = DateTime.UtcNow;

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                Amount = amount,
                PaymentMethod = method,
                PaidAt = DateTime.UtcNow,
                CreatedByUserId = userId,
                CreatedByUserName = userName
            };

            var table = await _db.DiningTables.FindAsync(order.TableId);
            if (table != null)
            {
                table.Status = TableStatus.Available;
                table.LastUpdatedByUserId = userId;
                table.LastUpdatedByUserName = userName;
                table.LastUpdatedAt = DateTime.UtcNow;
            }

            await _db.Payments.AddAsync(payment);
            await _db.SaveChangesAsync();

            return (order, payment, null);
        }
    }
}
