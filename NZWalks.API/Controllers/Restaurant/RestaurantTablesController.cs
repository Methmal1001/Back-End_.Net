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
    [Route("api/restaurant/Tables")]
    [ApiController]
    [Authorize]
    public class RestaurantTablesController : ControllerBase
    {
        private readonly ITableRepository _tableRepo;

        public RestaurantTablesController(ITableRepository tableRepo)
        {
            _tableRepo = tableRepo;
        }

        [HttpGet]
        [RequirePermission("RestaurantTables", "View")]
        public async Task<IActionResult> GetAll()
        {
            var tables = await _tableRepo.GetAllAsync();

            var result = tables.Select(t => new TableResponseDto
            {
                Id = t.table.Id,
                Name = t.table.Name,
                Section = t.table.Section,
                Capacity = t.table.Capacity,
                Status = t.table.Status.ToString(),
                OpenOrderId = t.openOrderId
            }).ToList();

            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = $"Retrieved {result.Count} table(s).", Data = result });
        }

        [HttpPost]
        [RequirePermission("RestaurantTables", "Manage")]
        public async Task<IActionResult> Create([FromBody] CreateTableRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            var (userId, userName) = GetCurrentUser();

            var table = new DiningTable
            {
                Name = dto.Name.Trim(),
                Section = dto.Section?.Trim(),
                Capacity = dto.Capacity,
                Status = TableStatus.Available,
                CreatedByUserId = userId,
                CreatedByUserName = userName
            };

            var created = await _tableRepo.CreateAsync(table);

            return Ok(new CommonApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = "Table created successfully.",
                Data = MapToDto(created, null)
            });
        }

        [HttpPut]
        [RequirePermission("RestaurantTables", "Manage")]
        public async Task<IActionResult> Update([FromBody] UpdateTableRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            if (!Enum.TryParse<TableStatus>(dto.Status, true, out var status))
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = $"Invalid status '{dto.Status}'. Valid values: {string.Join(", ", Enum.GetNames<TableStatus>())}.", Data = null });

            var (userId, userName) = GetCurrentUser();

            var updated = await _tableRepo.UpdateAsync(dto.Id, new DiningTable
            {
                Name = dto.Name.Trim(),
                Section = dto.Section?.Trim(),
                Capacity = dto.Capacity,
                Status = status,
                LastUpdatedByUserId = userId,
                LastUpdatedByUserName = userName
            });

            if (updated == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = $"Table with ID '{dto.Id}' was not found.", Data = null });

            var openOrderId = await _tableRepo.GetOpenOrderIdAsync(updated.Id);
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Table updated successfully.", Data = MapToDto(updated, openOrderId) });
        }

        [HttpDelete("{id:guid}")]
        [RequirePermission("RestaurantTables", "Manage")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var outcome = await _tableRepo.DeleteAsync(id);

            return outcome switch
            {
                TableDeleteOutcome.NotFound => NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = $"Table with ID '{id}' was not found.", Data = null }),
                TableDeleteOutcome.Blocked => Conflict(new CommonApiResponse<object> { StatusCode = 409, IsSuccess = false, Message = "This table has an open order and cannot be deleted until it is billed or cancelled.", Data = null }),
                _ => Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Table deleted successfully.", Data = null })
            };
        }

        [HttpPatch("{id:guid}/Status")]
        [RequirePermission("RestaurantTables", "Manage")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateTableStatusRequestDto dto)
        {
            if (!Enum.TryParse<TableStatus>(dto.Status, true, out var status))
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = $"Invalid status '{dto.Status}'. Valid values: {string.Join(", ", Enum.GetNames<TableStatus>())}.", Data = null });

            var (userId, userName) = GetCurrentUser();
            var updated = await _tableRepo.SetStatusAsync(id, status, userId, userName);

            if (updated == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = $"Table with ID '{id}' was not found.", Data = null });

            var openOrderId = await _tableRepo.GetOpenOrderIdAsync(updated.Id);
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = $"Table status updated to {status}.", Data = MapToDto(updated, openOrderId) });
        }

        private (Guid userId, string userName) GetCurrentUser()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var userId = Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
            var userName = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";
            return (userId, userName);
        }

        private static TableResponseDto MapToDto(DiningTable t, Guid? openOrderId) => new()
        {
            Id = t.Id,
            Name = t.Name,
            Section = t.Section,
            Capacity = t.Capacity,
            Status = t.Status.ToString(),
            OpenOrderId = openOrderId
        };
    }
}
