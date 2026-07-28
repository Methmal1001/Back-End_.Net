using Microsoft.EntityFrameworkCore;
using NZWalks.API.Data;
using NZWalks.API.Models.Domain.Restaurant;

namespace NZWalks.API.Repositories.Restaurant
{
    public class MenuRepository : IMenuRepository
    {
        private readonly RestaurantDbContext _db;

        public MenuRepository(RestaurantDbContext db)
        {
            _db = db;
        }

        // ── CATEGORIES ─────────────────────────────────────────────────────────
        public async Task<List<MenuCategory>> GetCategoriesAsync(bool? isActive)
        {
            var query = _db.MenuCategories.AsQueryable();

            if (isActive.HasValue)
                query = query.Where(c => c.IsActive == isActive.Value);

            return await query.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync();
        }

        public async Task<MenuCategory?> GetCategoryByIdAsync(Guid id)
        {
            return await _db.MenuCategories.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<int> GetItemCountForCategoryAsync(Guid categoryId)
        {
            return await _db.MenuItems.CountAsync(i => i.MenuCategoryId == categoryId);
        }

        public async Task<MenuCategory> CreateCategoryAsync(MenuCategory category)
        {
            category.Id = Guid.NewGuid();
            await _db.MenuCategories.AddAsync(category);
            await _db.SaveChangesAsync();
            return category;
        }

        public async Task<MenuCategory?> UpdateCategoryAsync(Guid id, MenuCategory updated)
        {
            var existing = await _db.MenuCategories.FindAsync(id);
            if (existing == null) return null;

            existing.Name = updated.Name;
            existing.Description = updated.Description;
            existing.DisplayOrder = updated.DisplayOrder;
            existing.IsActive = updated.IsActive;
            existing.LastUpdatedByUserId = updated.LastUpdatedByUserId;
            existing.LastUpdatedByUserName = updated.LastUpdatedByUserName;
            existing.LastUpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return existing;
        }

        public async Task<MenuDeleteOutcome?> DeleteCategoryAsync(Guid id)
        {
            var category = await _db.MenuCategories.FindAsync(id);
            if (category == null) return null;

            var itemCount = await GetItemCountForCategoryAsync(id);

            if (itemCount > 0)
            {
                category.IsActive = false;
                await _db.SaveChangesAsync();
                return new MenuDeleteOutcome { WasSoftDeleted = true, LinkedCount = itemCount };
            }

            _db.MenuCategories.Remove(category);
            await _db.SaveChangesAsync();
            return new MenuDeleteOutcome { WasSoftDeleted = false, LinkedCount = 0 };
        }

        // ── MENU ITEMS ────────────────────────────────────────────────────────
        public async Task<(List<MenuItem> items, int totalCount)> GetItemsAsync(
            Guid? categoryId, bool? isAvailable, int page, int pageSize)
        {
            var query = _db.MenuItems
                .Include(i => i.Category)
                .Include(i => i.ModifierGroups).ThenInclude(g => g.Options)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(i => i.MenuCategoryId == categoryId.Value);

            if (isAvailable.HasValue)
                query = query.Where(i => i.IsAvailable == isAvailable.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(i => i.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<MenuItem?> GetItemByIdAsync(Guid id)
        {
            return await _db.MenuItems
                .Include(i => i.Category)
                .Include(i => i.ModifierGroups).ThenInclude(g => g.Options)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<MenuItem> CreateItemAsync(MenuItem item, List<ModifierGroup> modifierGroups)
        {
            item.Id = Guid.NewGuid();

            foreach (var group in modifierGroups)
            {
                group.Id = Guid.NewGuid();
                group.MenuItemId = item.Id;
                foreach (var option in group.Options)
                    option.Id = Guid.NewGuid();
            }

            await _db.MenuItems.AddAsync(item);
            await _db.ModifierGroups.AddRangeAsync(modifierGroups);
            await _db.SaveChangesAsync();

            return (await GetItemByIdAsync(item.Id))!;
        }

        public async Task<MenuItem?> UpdateItemAsync(Guid id, MenuItem updated, List<ModifierGroup>? modifierGroups)
        {
            var existing = await _db.MenuItems
                .Include(i => i.ModifierGroups).ThenInclude(g => g.Options)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (existing == null) return null;

            existing.MenuCategoryId = updated.MenuCategoryId;
            existing.Name = updated.Name;
            existing.Description = updated.Description;
            existing.Price = updated.Price;
            existing.ImageUrl = updated.ImageUrl;
            existing.PrepTimeMinutes = updated.PrepTimeMinutes;
            existing.KitchenStation = updated.KitchenStation;
            existing.IsAvailable = updated.IsAvailable;
            existing.IsActive = updated.IsActive;
            existing.LastUpdatedByUserId = updated.LastUpdatedByUserId;
            existing.LastUpdatedByUserName = updated.LastUpdatedByUserName;
            existing.LastUpdatedAt = DateTime.UtcNow;

            if (modifierGroups != null)
            {
                _db.ModifierGroups.RemoveRange(existing.ModifierGroups);

                foreach (var group in modifierGroups)
                {
                    group.Id = Guid.NewGuid();
                    group.MenuItemId = id;
                    foreach (var option in group.Options)
                        option.Id = Guid.NewGuid();
                }

                await _db.ModifierGroups.AddRangeAsync(modifierGroups);
            }

            await _db.SaveChangesAsync();
            return await GetItemByIdAsync(id);
        }

        public async Task<MenuDeleteOutcome?> DeleteItemAsync(Guid id)
        {
            var item = await _db.MenuItems.FindAsync(id);
            if (item == null) return null;

            var orderCount = await _db.OrderItems.CountAsync(oi => oi.MenuItemId == id);

            if (orderCount > 0)
            {
                item.IsActive = false;
                item.IsAvailable = false;
                await _db.SaveChangesAsync();
                return new MenuDeleteOutcome { WasSoftDeleted = true, LinkedCount = orderCount };
            }

            _db.MenuItems.Remove(item);
            await _db.SaveChangesAsync();
            return new MenuDeleteOutcome { WasSoftDeleted = false, LinkedCount = 0 };
        }

        public async Task<MenuItem?> SetAvailabilityAsync(Guid id, bool isAvailable, Guid? userId, string? userName)
        {
            var item = await _db.MenuItems.FindAsync(id);
            if (item == null) return null;

            item.IsAvailable = isAvailable;
            item.LastUpdatedByUserId = userId;
            item.LastUpdatedByUserName = userName;
            item.LastUpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return await GetItemByIdAsync(id);
        }
    }
}
