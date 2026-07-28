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
    [Route("api/restaurant/Menu")]
    [ApiController]
    [Authorize]
    public class RestaurantMenuController : ControllerBase
    {
        private readonly IMenuRepository _menuRepo;

        public RestaurantMenuController(IMenuRepository menuRepo)
        {
            _menuRepo = menuRepo;
        }

        // ══════════════════════════════════════════════════════════════════════
        // CATEGORIES
        // ══════════════════════════════════════════════════════════════════════

        [HttpGet("Categories")]
        [RequirePermission("RestaurantMenu", "View")]
        public async Task<IActionResult> GetCategories([FromQuery] bool? isActive)
        {
            var categories = await _menuRepo.GetCategoriesAsync(isActive);
            var result = new List<MenuCategoryResponseDto>();

            foreach (var c in categories)
            {
                result.Add(new MenuCategoryResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    DisplayOrder = c.DisplayOrder,
                    IsActive = c.IsActive,
                    ItemCount = await _menuRepo.GetItemCountForCategoryAsync(c.Id)
                });
            }

            return Ok(new CommonApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = $"Retrieved {result.Count} menu categor{(result.Count == 1 ? "y" : "ies")}.",
                Data = result
            });
        }

        [HttpPost("Categories")]
        [RequirePermission("RestaurantMenu", "Create")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateMenuCategoryRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            var (userId, userName) = GetCurrentUser();

            var category = new MenuCategory
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                DisplayOrder = dto.DisplayOrder,
                IsActive = true,
                CreatedByUserId = userId,
                CreatedByUserName = userName
            };

            var created = await _menuRepo.CreateCategoryAsync(category);

            return Ok(new CommonApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = "Menu category created successfully.",
                Data = new MenuCategoryResponseDto
                {
                    Id = created.Id,
                    Name = created.Name,
                    Description = created.Description,
                    DisplayOrder = created.DisplayOrder,
                    IsActive = created.IsActive,
                    ItemCount = 0
                }
            });
        }

        [HttpPut("Categories")]
        [RequirePermission("RestaurantMenu", "Update")]
        public async Task<IActionResult> UpdateCategory([FromBody] UpdateMenuCategoryRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            var (userId, userName) = GetCurrentUser();

            var updated = await _menuRepo.UpdateCategoryAsync(dto.Id, new MenuCategory
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                DisplayOrder = dto.DisplayOrder,
                IsActive = dto.IsActive,
                LastUpdatedByUserId = userId,
                LastUpdatedByUserName = userName
            });

            if (updated == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = $"Menu category with ID '{dto.Id}' was not found.", Data = null });

            return Ok(new CommonApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = "Menu category updated successfully.",
                Data = new MenuCategoryResponseDto
                {
                    Id = updated.Id,
                    Name = updated.Name,
                    Description = updated.Description,
                    DisplayOrder = updated.DisplayOrder,
                    IsActive = updated.IsActive,
                    ItemCount = await _menuRepo.GetItemCountForCategoryAsync(updated.Id)
                }
            });
        }

        [HttpDelete("Categories/{id:guid}")]
        [RequirePermission("RestaurantMenu", "Delete")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var outcome = await _menuRepo.DeleteCategoryAsync(id);
            if (outcome == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = $"Menu category with ID '{id}' was not found.", Data = null });

            var message = outcome.WasSoftDeleted
                ? $"Category is referenced by {outcome.LinkedCount} menu item(s); it has been deactivated instead of removed."
                : "Menu category deleted successfully.";

            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = message, Data = null });
        }

        // ══════════════════════════════════════════════════════════════════════
        // MENU ITEMS
        // ══════════════════════════════════════════════════════════════════════

        [HttpGet("Items")]
        [RequirePermission("RestaurantMenu", "View")]
        public async Task<IActionResult> GetItems(
            [FromQuery] Guid? categoryId,
            [FromQuery] bool? isAvailable,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var (items, totalCount) = await _menuRepo.GetItemsAsync(categoryId, isAvailable, page, pageSize);

            var response = new MenuItemListResponseDto
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Items = items.Select(MapToItemDto).ToList()
            };

            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Menu items retrieved successfully.", Data = response });
        }

        [HttpGet("Items/{id:guid}")]
        [RequirePermission("RestaurantMenu", "View")]
        public async Task<IActionResult> GetItemById(Guid id)
        {
            var item = await _menuRepo.GetItemByIdAsync(id);
            if (item == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = $"Menu item with ID '{id}' was not found.", Data = null });

            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Menu item retrieved successfully.", Data = MapToItemDto(item) });
        }

        [HttpPost("Items")]
        [RequirePermission("RestaurantMenu", "Create")]
        public async Task<IActionResult> CreateItem([FromBody] CreateMenuItemRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            if (!Enum.TryParse<KitchenStation>(dto.KitchenStation, true, out var station))
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = $"Invalid kitchen station '{dto.KitchenStation}'. Valid values: {string.Join(", ", Enum.GetNames<KitchenStation>())}.", Data = null });

            var category = await _menuRepo.GetCategoryByIdAsync(dto.MenuCategoryId);
            if (category == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = $"Menu category with ID '{dto.MenuCategoryId}' was not found.", Data = null });

            var (userId, userName) = GetCurrentUser();

            var item = new MenuItem
            {
                MenuCategoryId = dto.MenuCategoryId,
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                Price = dto.Price,
                ImageUrl = dto.ImageUrl?.Trim(),
                PrepTimeMinutes = dto.PrepTimeMinutes,
                KitchenStation = station,
                IsAvailable = true,
                IsActive = true,
                CreatedByUserId = userId,
                CreatedByUserName = userName
            };

            var groups = MapModifierGroups(dto.ModifierGroups);

            var created = await _menuRepo.CreateItemAsync(item, groups);

            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Menu item created successfully.", Data = MapToItemDto(created) });
        }

        [HttpPut("Items")]
        [RequirePermission("RestaurantMenu", "Update")]
        public async Task<IActionResult> UpdateItem([FromBody] UpdateMenuItemRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            if (!Enum.TryParse<KitchenStation>(dto.KitchenStation, true, out var station))
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = $"Invalid kitchen station '{dto.KitchenStation}'. Valid values: {string.Join(", ", Enum.GetNames<KitchenStation>())}.", Data = null });

            var category = await _menuRepo.GetCategoryByIdAsync(dto.MenuCategoryId);
            if (category == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = $"Menu category with ID '{dto.MenuCategoryId}' was not found.", Data = null });

            var (userId, userName) = GetCurrentUser();

            var updated = new MenuItem
            {
                MenuCategoryId = dto.MenuCategoryId,
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                Price = dto.Price,
                ImageUrl = dto.ImageUrl?.Trim(),
                PrepTimeMinutes = dto.PrepTimeMinutes,
                KitchenStation = station,
                IsAvailable = dto.IsAvailable,
                IsActive = dto.IsActive,
                LastUpdatedByUserId = userId,
                LastUpdatedByUserName = userName
            };

            var groups = dto.ModifierGroups != null ? MapModifierGroups(dto.ModifierGroups) : null;

            var result = await _menuRepo.UpdateItemAsync(dto.Id, updated, groups);
            if (result == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = $"Menu item with ID '{dto.Id}' was not found.", Data = null });

            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Menu item updated successfully.", Data = MapToItemDto(result) });
        }

        [HttpDelete("Items/{id:guid}")]
        [RequirePermission("RestaurantMenu", "Delete")]
        public async Task<IActionResult> DeleteItem(Guid id)
        {
            var outcome = await _menuRepo.DeleteItemAsync(id);
            if (outcome == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = $"Menu item with ID '{id}' was not found.", Data = null });

            var message = outcome.WasSoftDeleted
                ? $"Item has been ordered {outcome.LinkedCount} time(s) and cannot be removed; it has been deactivated and 86'd instead."
                : "Menu item deleted successfully.";

            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = message, Data = null });
        }

        [HttpPatch("Items/{id:guid}/Availability")]
        [RequirePermission("RestaurantMenu", "Update")]
        public async Task<IActionResult> UpdateAvailability(Guid id, [FromBody] UpdateMenuItemAvailabilityRequestDto dto)
        {
            var (userId, userName) = GetCurrentUser();
            var result = await _menuRepo.SetAvailabilityAsync(id, dto.IsAvailable, userId, userName);

            if (result == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = $"Menu item with ID '{id}' was not found.", Data = null });

            return Ok(new CommonApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = dto.IsAvailable ? "Item marked available." : "Item marked unavailable (86'd).",
                Data = MapToItemDto(result)
            });
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

        private static List<ModifierGroup> MapModifierGroups(List<ModifierGroupRequestDto>? groups)
        {
            if (groups == null) return new List<ModifierGroup>();

            return groups.Select(g => new ModifierGroup
            {
                Name = g.Name.Trim(),
                IsRequired = g.IsRequired,
                MinSelect = g.MinSelect,
                MaxSelect = g.MaxSelect,
                Options = g.Options.Select(o => new ModifierOption
                {
                    Name = o.Name.Trim(),
                    PriceDelta = o.PriceDelta
                }).ToList()
            }).ToList();
        }

        private static MenuItemResponseDto MapToItemDto(MenuItem item) => new()
        {
            Id = item.Id,
            MenuCategoryId = item.MenuCategoryId,
            CategoryName = item.Category?.Name ?? string.Empty,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            ImageUrl = item.ImageUrl,
            PrepTimeMinutes = item.PrepTimeMinutes,
            KitchenStation = item.KitchenStation.ToString(),
            IsAvailable = item.IsAvailable,
            IsActive = item.IsActive,
            ModifierGroups = item.ModifierGroups.Select(g => new ModifierGroupResponseDto
            {
                Id = g.Id,
                Name = g.Name,
                IsRequired = g.IsRequired,
                MinSelect = g.MinSelect,
                MaxSelect = g.MaxSelect,
                Options = g.Options.Select(o => new ModifierOptionResponseDto
                {
                    Id = o.Id,
                    Name = o.Name,
                    PriceDelta = o.PriceDelta
                }).ToList()
            }).ToList()
        };
    }
}
