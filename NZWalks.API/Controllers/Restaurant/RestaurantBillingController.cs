using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NZWalks.API.Helpers;
using NZWalks.API.Models.Domain.Restaurant;
using NZWalks.API.Models.DTO.Restaurant;
using NZWalks.API.Repositories.Restaurant;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace NZWalks.API.Controllers.Restaurant
{
    [Route("api/restaurant/Billing")]
    [ApiController]
    [Authorize]
    public class RestaurantBillingController : ControllerBase
    {
        private readonly IBillingRepository _billingRepo;

        public RestaurantBillingController(IBillingRepository billingRepo)
        {
            _billingRepo = billingRepo;
        }

        // ══════════════════════════════════════════════════════════════════════
        // GET  api/restaurant/Billing/{orderId} — itemized bill preview
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("{orderId:guid}")]
        [RequirePermission("RestaurantBilling", "View")]
        public async Task<IActionResult> GetBill(Guid orderId, [FromQuery] decimal discountAmount = 0)
        {
            var order = await _billingRepo.GetOrderForBillingAsync(orderId);
            if (order == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = $"Order with ID '{orderId}' was not found.", Data = null });

            var bill = _billingRepo.ComputeBill(order, discountAmount);

            return Ok(new CommonApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = "Bill preview generated.",
                Data = MapToBillDto(order, bill)
            });
        }

        // ══════════════════════════════════════════════════════════════════════
        // POST  api/restaurant/Billing/{orderId}/Pay
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("{orderId:guid}/Pay")]
        [RequirePermission("RestaurantBilling", "ProcessPayment")]
        public async Task<IActionResult> Pay(Guid orderId, [FromBody] ProcessPaymentRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            if (!Enum.TryParse<PaymentMethod>(dto.PaymentMethod, true, out var method))
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = $"Invalid payment method '{dto.PaymentMethod}'. Valid values: {string.Join(", ", Enum.GetNames<PaymentMethod>())}.", Data = null });

            var (userId, userName) = GetCurrentUser();
            var (order, payment, error) = await _billingRepo.ProcessPaymentAsync(orderId, dto.Amount, method, dto.DiscountAmount, userId, userName);

            if (error != null)
            {
                var statusCode = error.Contains("not found") ? 404 : 400;
                return StatusCode(statusCode, new CommonApiResponse<object> { StatusCode = statusCode, IsSuccess = false, Message = error, Data = null });
            }

            var bill = _billingRepo.ComputeBill(order!, order!.DiscountAmount);

            return Ok(new CommonApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = "Payment recorded; order closed and table freed.",
                Data = new PaymentResultResponseDto
                {
                    Payment = new PaymentResponseDto
                    {
                        Id = payment!.Id,
                        OrderId = payment.OrderId,
                        Amount = payment.Amount,
                        PaymentMethod = payment.PaymentMethod.ToString(),
                        PaidAt = payment.PaidAt
                    },
                    Bill = MapToBillDto(order, bill),
                    OrderStatus = order.Status.ToString()
                }
            });
        }

        private (Guid userId, string userName) GetCurrentUser()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var userId = Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
            var userName = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";
            return (userId, userName);
        }

        private static BillResponseDto MapToBillDto(Order order, BillComputation bill) => new()
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            TableName = order.Table?.Name ?? string.Empty,
            Items = order.Items
                .Where(i => i.Status != OrderItemStatus.Cancelled)
                .Select(i => new BillLineItemDto
                {
                    MenuItemName = i.MenuItem?.Name ?? string.Empty,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.UnitPrice * i.Quantity
                }).ToList(),
            Subtotal = bill.Subtotal,
            TaxAmount = bill.TaxAmount,
            ServiceChargeAmount = bill.ServiceChargeAmount,
            DiscountAmount = bill.DiscountAmount,
            TotalAmount = bill.TotalAmount
        };
    }
}
