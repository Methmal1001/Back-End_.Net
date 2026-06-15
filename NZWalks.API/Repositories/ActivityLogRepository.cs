using Microsoft.EntityFrameworkCore;
using NZWalks.API.Data;
using NZWalks.API.Models.Domain.Inventory;

namespace NZWalks.API.Repositories
{
    public class ActivityLogRepository : IActivityLogRepository
    {
        private readonly InventoryDbContext _db;

        public ActivityLogRepository(InventoryDbContext db) => _db = db;

        public async Task<(List<ActivityLog> logs, int totalCount)> GetAllAsync(
            Guid? userId,
            string? module,
            string? action,
            Guid? entityId,
            bool? isSuccess,
            DateTime? dateFrom,
            DateTime? dateTo,
            int page,
            int pageSize)
        {
            var query = _db.ActivityLogs.AsQueryable();

            if (userId.HasValue)
                query = query.Where(a => a.UserId == userId.Value);

            if (!string.IsNullOrWhiteSpace(module))
                query = query.Where(a => a.Module == module);

            if (!string.IsNullOrWhiteSpace(action))
                query = query.Where(a => a.Action == action);

            if (entityId.HasValue)
                query = query.Where(a => a.EntityId == entityId.Value);

            if (isSuccess.HasValue)
                query = query.Where(a => a.IsSuccess == isSuccess.Value);

            if (dateFrom.HasValue)
                query = query.Where(a => a.CreatedAt >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(a => a.CreatedAt <= dateTo.Value);

            var totalCount = await query.CountAsync();

            var logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (logs, totalCount);
        }
    }
}
