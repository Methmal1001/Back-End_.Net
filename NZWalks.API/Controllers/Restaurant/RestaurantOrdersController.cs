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
    [Route("api/restaurant/Orders")]
    [ApiController]
    [Authorize]
    public class RestaurantOrdersController : ControllerBase
    {
        private readonly IOrderRepository _orderRepo;

        public RestaurantOrdersController(IOrderRepository orderRepo)
        {
            _orderRepo = orderRepo;
        }

        // ══════════════════════════════════════════════════════════════════════
        // POST  api/restaurant/Orders — open a new order for a table
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost]
        [RequirePermission("RestaurantOrders", "Create")]
        public async Task<IActionResult> Create([FromBody] CreateOrderRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            var (userId, userName) = GetCurrentUser();
            var (order, error) = await _orderRepo.CreateOrderAsync(dto.TableId, userId, userName);

            if (error != null)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = error, Data = null });

            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Order opened successfully.", Data = MapToOrderDto(order!) });
        }

        // ══════════════════════════════════════════════════════════════════════
        // GET  api/restaurant/Orders/{id}
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("{id:guid}")]
        [RequirePermission("RestaurantOrders", "View")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = $"Order with ID '{id}' was not found.", Data = null });

            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Order retrieved successfully.", Data = MapToOrderDto(order) });
        }

        // ══════════════════════════════════════════════════════════════════════
        // GET  api/restaurant/Orders?status=&tableId=&page=&pageSize=
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet]
        [RequirePermission("RestaurantOrders", "View")]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? status,
            [FromQuery] Guid? tableId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            OrderStatus? parsedStatus = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<OrderStatus>(status, true, out var s))
                    return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = $"Invalid status '{status}'. Valid values: {string.Join(", ", Enum.GetNames<OrderStatus>())}.", Data = null });
                parsedStatus = s;
            }

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var (orders, totalCount) = await _orderRepo.GetAllAsync(parsedStatus, tableId, page, pageSize);

            var response = new OrderListResponseDto
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Orders = orders.Select(MapToSummaryDto).ToList()
            };

            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Orders retrieved successfully.", Data = response });
        }

        // ══════════════════════════════════════════════════════════════════════
        // POST  api/restaurant/Orders/{id}/Items
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("{id:guid}/Items")]
        [RequirePermission("RestaurantOrders", "Create")]
        public async Task<IActionResult> AddItem(Guid id, [FromBody] AddOrderItemRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            var menuItem = await _orderRepo.GetMenuItemForOrderingAsync(dto.MenuItemId);
            if (menuItem == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = $"Menu item with ID '{dto.MenuItemId}' was not found.", Data = null });

            if (!menuItem.IsAvailable || !menuItem.IsActive)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = $"'{menuItem.Name}' is not currently available.", Data = null });

            var (item, error) = await _orderRepo.AddItemAsync(id, menuItem, dto.Quantity, dto.SpecialInstructions?.Trim(), dto.SelectedModifierOptionIds);
            if (error != null)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = error, Data = null });

            var order = await _orderRepo.GetByIdAsync(id);
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Item added to order.", Data = MapToOrderDto(order!) });
        }

        // ══════════════════════════════════════════════════════════════════════
        // PUT  api/restaurant/Orders/{id}/Items/{itemId}
        // ══════════════════════════════════════════════════════════════════════
        [HttpPut("{id:guid}/Items/{itemId:guid}")]
        [RequirePermission("RestaurantOrders", "Update")]
        public async Task<IActionResult> UpdateItem(Guid id, Guid itemId, [FromBody] UpdateOrderItemRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            var existingItem = await _orderRepo.GetOrderItemAsync(id, itemId);
            if (existingItem == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = $"Order item with ID '{itemId}' was not found on this order.", Data = null });

            var menuItem = await _orderRepo.GetMenuItemForOrderingAsync(existingItem.MenuItemId);

            var (item, error) = await _orderRepo.UpdateItemAsync(id, itemId, menuItem!, dto.Quantity, dto.SpecialInstructions?.Trim(), dto.SelectedModifierOptionIds);
            if (error != null)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = error, Data = null });

            var order = await _orderRepo.GetByIdAsync(id);
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Order item updated.", Data = MapToOrderDto(order!) });
        }

        // ══════════════════════════════════════════════════════════════════════
        // DELETE  api/restaurant/Orders/{id}/Items/{itemId}
        // ══════════════════════════════════════════════════════════════════════
        [HttpDelete("{id:guid}/Items/{itemId:guid}")]
        [RequirePermission("RestaurantOrders", "Update")]
        public async Task<IActionResult> RemoveItem(Guid id, Guid itemId)
        {
            var (success, error) = await _orderRepo.RemoveItemAsync(id, itemId);
            if (!success)
            {
                var statusCode = error != null && error.Contains("not found") ? 404 : 400;
                return StatusCode(statusCode, new CommonApiResponse<object> { StatusCode = statusCode, IsSuccess = false, Message = error, Data = null });
            }

            var order = await _orderRepo.GetByIdAsync(id);
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Order item removed.", Data = MapToOrderDto(order!) });
        }

        // ══════════════════════════════════════════════════════════════════════
        // POST  api/restaurant/Orders/{id}/Send
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("{id:guid}/Send")]
        [RequirePermission("RestaurantOrders", "Update")]
        public async Task<IActionResult> Send(Guid id)
        {
            var (userId, userName) = GetCurrentUser();
            var (order, error) = await _orderRepo.SendToKitchenAsync(id, userId, userName);

            if (error != null)
            {
                var statusCode = error.Contains("not found") ? 404 : 400;
                return StatusCode(statusCode, new CommonApiResponse<object> { StatusCode = statusCode, IsSuccess = false, Message = error, Data = null });
            }

            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Order sent to the kitchen.", Data = MapToOrderDto(order!) });
        }

        // ══════════════════════════════════════════════════════════════════════
        // POST  api/restaurant/Orders/{id}/Cancel
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("{id:guid}/Cancel")]
        [RequirePermission("RestaurantOrders", "Cancel")]
        public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelOrderRequestDto? dto)
        {
            var (userId, userName) = GetCurrentUser();
            var (order, error) = await _orderRepo.CancelOrderAsync(id, userId, userName);

            if (error != null)
            {
                var statusCode = error.Contains("not found") ? 404 : 400;
                return StatusCode(statusCode, new CommonApiResponse<object> { StatusCode = statusCode, IsSuccess = false, Message = error, Data = null });
            }

            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Order cancelled.", Data = MapToOrderDto(order!) });
        }

        // ══════════════════════════════════════════════════════════════════════
        // GET  api/restaurant/Orders/ReadyToServe
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet("ReadyToServe")]
        [RequirePermission("RestaurantKitchen", "View")]
        public async Task<IActionResult> ReadyToServe()
        {
            var orders = await _orderRepo.GetReadyToServeAsync();
            var result = orders.Select(MapToOrderDto).ToList();

            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = $"Retrieved {result.Count} order(s) ready to serve.", Data = result });
        }

        // ══════════════════════════════════════════════════════════════════════
        // PATCH  api/restaurant/Orders/{id}/Items/{itemId}/Serve
        // ══════════════════════════════════════════════════════════════════════
        [HttpPatch("{id:guid}/Items/{itemId:guid}/Serve")]
        [RequirePermission("RestaurantKitchen", "UpdateStatus")]
        public async Task<IActionResult> ServeItem(Guid id, Guid itemId)
        {
            var (item, error) = await _orderRepo.MarkItemServedAsync(id, itemId);
            if (error != null)
            {
                var statusCode = error.Contains("not found") ? 404 : 400;
                return StatusCode(statusCode, new CommonApiResponse<object> { StatusCode = statusCode, IsSuccess = false, Message = error, Data = null });
            }

            var order = await _orderRepo.GetByIdAsync(id);
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Item marked as served.", Data = MapToOrderDto(order!) });
        }

        // ══════════════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ══════════════════════════════════════════════════════════════════════
        private (Guid userId, string userName) GetCurrentUser()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var userId = Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
            var userName = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";
            return (userId, userName);
        }

        internal static OrderResponseDto MapToOrderDto(Order o) => new()
        {
            Id = o.Id,
            TableId = o.TableId,
            TableName = o.Table?.Name ?? string.Empty,
            OrderNumber = o.OrderNumber,
            Status = o.Status.ToString(),
            Subtotal = o.Subtotal,
            TaxAmount = o.TaxAmount,
            ServiceChargeAmount = o.ServiceChargeAmount,
            DiscountAmount = o.DiscountAmount,
            TotalAmount = o.TotalAmount,
            OpenedAt = o.OpenedAt,
            ClosedAt = o.ClosedAt,
            CreatedByUserName = o.CreatedByUserName,
            LastUpdatedByUserName = o.LastUpdatedByUserName,
            Items = o.Items.Select(MapToItemDto).ToList()
        };

        internal static OrderItemResponseDto MapToItemDto(OrderItem i) => new()
        {
            Id = i.Id,
            MenuItemId = i.MenuItemId,
            MenuItemName = i.MenuItem?.Name ?? string.Empty,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            LineTotal = i.UnitPrice * i.Quantity,
            SpecialInstructions = i.SpecialInstructions,
            Status = i.Status.ToString(),
            KitchenStation = i.KitchenStation.ToString(),
            Modifiers = i.Modifiers.Select(m => new OrderItemModifierResponseDto
            {
                Id = m.Id,
                ModifierOptionId = m.ModifierOptionId,
                ModifierGroupName = m.ModifierGroupName,
                ModifierOptionName = m.ModifierOptionName,
                PriceDelta = m.PriceDelta
            }).ToList()
        };

        private static OrderSummaryResponseDto MapToSummaryDto(Order o) => new()
        {
            Id = o.Id,
            TableId = o.TableId,
            TableName = o.Table?.Name ?? string.Empty,
            OrderNumber = o.OrderNumber,
            Status = o.Status.ToString(),
            TotalAmount = o.TotalAmount,
            ItemCount = o.Items.Count,
            OpenedAt = o.OpenedAt
        };
    }
}
