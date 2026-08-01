using NZWalks.API.Models.Domain.Restaurant;

namespace NZWalks.API.Repositories.Restaurant
{
    public enum TableDeleteOutcome { NotFound, Blocked, Deleted }

    public interface ITableRepository
    {
        Task<List<(DiningTable table, Guid? openOrderId)>> GetAllAsync();
        Task<DiningTable?> GetByIdAsync(Guid id);
        Task<DiningTable> CreateAsync(DiningTable table);
        Task<DiningTable?> UpdateAsync(Guid id, DiningTable updated);
        Task<TableDeleteOutcome> DeleteAsync(Guid id);
        Task<DiningTable?> SetStatusAsync(Guid id, TableStatus status, Guid? userId, string? userName);
        Task<Guid?> GetOpenOrderIdAsync(Guid tableId);
    }
}
