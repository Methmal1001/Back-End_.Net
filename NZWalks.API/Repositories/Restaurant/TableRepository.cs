using Microsoft.EntityFrameworkCore;
using NZWalks.API.Data;
using NZWalks.API.Models.Domain.Restaurant;

namespace NZWalks.API.Repositories.Restaurant
{
    public class TableRepository : ITableRepository
    {
        private static readonly OrderStatus[] OpenOrderStatuses =
        {
            OrderStatus.Open, OrderStatus.SentToKitchen, OrderStatus.InProgress,
            OrderStatus.ReadyToServe, OrderStatus.Served, OrderStatus.Billed
        };

        private readonly RestaurantDbContext _db;

        public TableRepository(RestaurantDbContext db)
        {
            _db = db;
        }

        public async Task<List<(DiningTable table, Guid? openOrderId)>> GetAllAsync()
        {
            var tables = await _db.DiningTables.OrderBy(t => t.Name).ToListAsync();

            var openOrders = await _db.Orders
                .Where(o => OpenOrderStatuses.Contains(o.Status))
                .ToDictionaryAsync(o => o.TableId, o => o.Id);

            return tables
                .Select(t => (t, openOrders.TryGetValue(t.Id, out var orderId) ? (Guid?)orderId : null))
                .ToList();
        }

        public async Task<DiningTable?> GetByIdAsync(Guid id)
        {
            return await _db.DiningTables.FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<DiningTable> CreateAsync(DiningTable table)
        {
            table.Id = Guid.NewGuid();
            await _db.DiningTables.AddAsync(table);
            await _db.SaveChangesAsync();
            return table;
        }

        public async Task<DiningTable?> UpdateAsync(Guid id, DiningTable updated)
        {
            var existing = await _db.DiningTables.FindAsync(id);
            if (existing == null) return null;

            existing.Name = updated.Name;
            existing.Section = updated.Section;
            existing.Capacity = updated.Capacity;
            existing.Status = updated.Status;
            existing.LastUpdatedByUserId = updated.LastUpdatedByUserId;
            existing.LastUpdatedByUserName = updated.LastUpdatedByUserName;
            existing.LastUpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return existing;
        }

        public async Task<TableDeleteOutcome> DeleteAsync(Guid id)
        {
            var table = await _db.DiningTables.FindAsync(id);
            if (table == null) return TableDeleteOutcome.NotFound;

            var hasOpenOrder = await _db.Orders.AnyAsync(o => o.TableId == id && OpenOrderStatuses.Contains(o.Status));
            if (hasOpenOrder) return TableDeleteOutcome.Blocked;

            _db.DiningTables.Remove(table);
            await _db.SaveChangesAsync();
            return TableDeleteOutcome.Deleted;
        }

        public async Task<DiningTable?> SetStatusAsync(Guid id, TableStatus status, Guid? userId, string? userName)
        {
            var table = await _db.DiningTables.FindAsync(id);
            if (table == null) return null;

            table.Status = status;
            table.LastUpdatedByUserId = userId;
            table.LastUpdatedByUserName = userName;
            table.LastUpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return table;
        }

        public async Task<Guid?> GetOpenOrderIdAsync(Guid tableId)
        {
            var order = await _db.Orders
                .Where(o => o.TableId == tableId && OpenOrderStatuses.Contains(o.Status))
                .Select(o => (Guid?)o.Id)
                .FirstOrDefaultAsync();

            return order;
        }
    }
}
