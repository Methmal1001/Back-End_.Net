using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NZWalks.API.Helpers;
using NZWalks.API.Models.DTO.ActivityLog;
using NZWalks.API.Repositories;

namespace NZWalks.API.Controllers
{
    [Route("api/activity-logs")]
    [ApiController]
    [Authorize]
    public class ActivityLogsController : ControllerBase
    {
        private readonly IActivityLogRepository _repo;

        public ActivityLogsController(IActivityLogRepository repo)
        {
            _repo = repo;
        }

        // ══════════════════════════════════════════════════════════════════════
        // GET  api/activity-logs
        // Query params: userId, module, action, entityId, isSuccess, dateFrom, dateTo, page, pageSize
        // ══════════════════════════════════════════════════════════════════════
        [HttpGet]
        [RequirePermission("System", "ViewActivityLogs")]
        public async Task<IActionResult> GetAll(
            [FromQuery] Guid? userId,
            [FromQuery] string? module,
            [FromQuery] string? action,
            [FromQuery] Guid? entityId,
            [FromQuery] bool? isSuccess,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var (logs, totalCount) = await _repo.GetAllAsync(
                userId, module, action, entityId, isSuccess, dateFrom, dateTo, page, pageSize);

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var response = new ActivityLogListResponseDto
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                Items = logs.Select(MapToResponseDto).ToList()
            };

            return Ok(new CommonApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = $"Retrieved {logs.Count} of {totalCount} activity log(s) (page {page}/{Math.Max(totalPages, 1)}).",
                Data = response
            });
        }

        private static ActivityLogResponseDto MapToResponseDto(Models.Domain.Inventory.ActivityLog a) => new()
        {
            Id = a.Id,
            UserId = a.UserId,
            UserName = a.UserName,
            UserRole = a.UserRole,
            Module = a.Module,
            Action = a.Action,
            HttpMethod = a.HttpMethod,
            Path = a.Path,
            EntityId = a.EntityId,
            Summary = a.Summary,
            StatusCode = a.StatusCode,
            IsSuccess = a.IsSuccess,
            Details = a.Details,
            IpAddress = a.IpAddress,
            CreatedAt = a.CreatedAt
        };
    }
}
