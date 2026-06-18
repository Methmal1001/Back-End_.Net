using NZWalks.API.Models.Domain.Inventory;

namespace NZWalks.API.Repositories
{
    public interface IActivityLogRepository
    {
        Task<(List<ActivityLog> logs, int totalCount)> GetAllAsync(
            Guid? userId,
            string? module,
            string? action,
            Guid? entityId,
            bool? isSuccess,
            DateTime? dateFrom,
            DateTime? dateTo,
            int page,
            int pageSize);
    }
}
