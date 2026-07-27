using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using NZWalks.API.Data;
using NZWalks.API.Models.Domain.Inventory;

namespace NZWalks.API.Filters
{
    // The global ActivityLogFilter only logs mutating (POST/PUT/PATCH/DELETE)
    // requests. Report endpoints are all GETs, but report access still needs
    // to be attributable to a user, so this logs every report request
    // (view or export) to the same ActivityLogs table.
    public class ReportAccessLogFilter : IAsyncActionFilter
    {
        private readonly InventoryDbContext _db;
        private readonly ILogger<ReportAccessLogFilter> _logger;

        public ReportAccessLogFilter(InventoryDbContext db, ILogger<ReportAccessLogFilter> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var executedContext = await next();

            try
            {
                await WriteAccessLogAsync(context, executedContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write ActivityLog row for report access {Path}",
                    context.HttpContext.Request.Path);
            }
        }

        private async Task WriteAccessLogAsync(ActionExecutingContext context, ActionExecutedContext executedContext)
        {
            var httpContext = context.HttpContext;
            var user = httpContext.User;

            var subClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
            var userId = Guid.TryParse(subClaim, out var id) ? id : (Guid?)null;
            var userName = user.FindFirst(ClaimTypes.Name)?.Value ?? user.FindFirst("email")?.Value;
            var userRole = user.FindFirst("roleName")?.Value;

            var action = context.ActionDescriptor is ControllerActionDescriptor cad ? cad.ActionName : "Unknown";
            var format = context.ActionArguments.TryGetValue("format", out var f) ? f?.ToString() ?? "json" : "json";

            var statusCode = executedContext.Exception != null && !executedContext.ExceptionHandled
                ? 500
                : (executedContext.Result as ObjectResult)?.StatusCode
                    ?? (executedContext.Result as StatusCodeResult)?.StatusCode
                    ?? 200;

            _db.ActivityLogs.Add(new ActivityLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserName = userName,
                UserRole = userRole,
                Module = "Reports",
                Action = action,
                HttpMethod = httpContext.Request.Method,
                Path = Truncate($"{httpContext.Request.Path}{httpContext.Request.QueryString}", 500),
                EntityId = null,
                Summary = Truncate($"Reports.{action}: report requested (format={format})", 1000),
                StatusCode = statusCode,
                IsSuccess = statusCode is >= 200 and < 300,
                Details = null,
                IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }

        private static string Truncate(string value, int maxLength)
            => string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
    }
}
