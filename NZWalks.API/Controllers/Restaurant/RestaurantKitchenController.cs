using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NZWalks.API.Helpers;
using NZWalks.API.Models.Domain.Restaurant;
using NZWalks.API.Models.DTO.Restaurant;
using NZWalks.API.Repositories.Restaurant;

namespace NZWalks.API.Controllers.Restaurant
{
    [Route("api/restaurant/Kitchen")]
    [ApiController]
    [Authorize]
    public class RestaurantKitchenController : ControllerBase
    {
        private readonly IOrderRepository _orderRepo;

        public RestaurantKitchenController(IOrderRepository orderRepo)
        {
            _orderRepo = orderRepo;
        }

        // ══════════════════════════════════════════════════════════════════════
        // GET  api/restaurant/Kitchen/Tickets?station=&status=
        // Active order items across all sent orders, grouped by order/table.
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("Tickets")]
        [RequirePermission("RestaurantKitchen", "View")]
        public async Task<IActionResult> GetTickets([FromQuery] string? station, [FromQuery] string? status)
        {
            KitchenStation? parsedStation = null;
            if (!string.IsNullOrWhiteSpace(station))
            {
                if (!Enum.TryParse<KitchenStation>(station, true, out var s))
                    return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = $"Invalid station '{station}'. Valid values: {string.Join(", ", Enum.GetNames<KitchenStation>())}.", Data = null });
                parsedStation = s;
            }

            OrderItemStatus? parsedStatus = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<OrderItemStatus>(status, true, out var st))
                    return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = $"Invalid status '{status}'. Valid values: {string.Join(", ", Enum.GetNames<OrderItemStatus>())}.", Data = null });
                parsedStatus = st;
            }

            var items = await _orderRepo.GetKitchenTicketItemsAsync(parsedStation);

            if (parsedStatus.HasValue)
                items = items.Where(i => i.Status == parsedStatus.Value).ToList();

            var tickets = items
                .GroupBy(i => i.Order)
                .OrderBy(g => g.Key.OpenedAt)
                .Select(g => new KitchenTicketDto
                {
                    OrderId = g.Key.Id,
                    OrderNumber = g.Key.OrderNumber,
                    TableName = g.Key.Table?.Name ?? string.Empty,
                    OpenedAt = g.Key.OpenedAt,
                    Items = g.Select(i => new KitchenTicketItemDto
                    {
                        OrderItemId = i.Id,
                        MenuItemName = i.MenuItem?.Name ?? string.Empty,
                        Quantity = i.Quantity,
                        SpecialInstructions = i.SpecialInstructions,
                        Status = i.Status.ToString(),
                        KitchenStation = i.KitchenStation.ToString(),
                        Modifiers = i.Modifiers.Select(m => m.ModifierOptionName).ToList()
                    }).ToList()
                })
                .ToList();

            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = $"Retrieved {tickets.Count} active ticket(s).", Data = tickets });
        }

        // ══════════════════════════════════════════════════════════════════════
        // PATCH  api/restaurant/Kitchen/Items/{itemId}/Status
        // Advance an item's status: Pending → Preparing → Ready.
        // ══════════════════════════════════════════════════════════════════════
        [HttpPatch("Items/{itemId:guid}/Status")]
        [RequirePermission("RestaurantKitchen", "UpdateStatus")]
        public async Task<IActionResult> UpdateItemStatus(Guid itemId, [FromBody] UpdateItemStatusRequestDto dto)
        {
            if (!Enum.TryParse<OrderItemStatus>(dto.Status, true, out var newStatus))
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = $"Invalid status '{dto.Status}'. Valid values: {string.Join(", ", Enum.GetNames<OrderItemStatus>())}.", Data = null });

            var (item, error) = await _orderRepo.AdvanceKitchenStatusAsync(itemId, newStatus);
            if (error != null)
            {
                var statusCode = error.Contains("not found") ? 404 : 400;
                return StatusCode(statusCode, new CommonApiResponse<object> { StatusCode = statusCode, IsSuccess = false, Message = error, Data = null });
            }

            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = $"Item status updated to {newStatus}.", Data = RestaurantOrdersController.MapToItemDto(item!) });
        }
    }
}
