using Microsoft.EntityFrameworkCore;
using NZWalks.API.Data;
using NZWalks.API.Models.Domain.Inventory;

namespace NZWalks.API.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly InventoryDbContext _db;

        public CategoryRepository(InventoryDbContext db)
        {
            _db = db;
        }

        // ── GET ALL (optional isActive filter, plus sub-category/product counts) ──
        public async Task<(List<Category> categories, Dictionary<Guid, int> subCategoryCounts, Dictionary<Guid, int> productCounts)> GetAllAsync(bool? isActive)
        {
            var query = _db.Categories
                .Include(c => c.ParentCategory)
                .AsQueryable();

            if (isActive.HasValue)
                query = query.Where(c => c.IsActive == isActive.Value);

            var categories = await query.OrderBy(c => c.Name).ToListAsync();

            var subCategoryCounts = await _db.Categories
                .Where(c => c.ParentCategoryId != null)
                .GroupBy(c => c.ParentCategoryId!.Value)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count);

            var productCounts = await _db.Products
                .GroupBy(p => p.CategoryId)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count);

            return (categories, subCategoryCounts, productCounts);
        }

        // ── GET BY ID ─────────────────────────────────────────────────────────
        public async Task<Category?> GetByIdAsync(Guid id)
        {
            return await _db.Categories
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        // ── CREATE ────────────────────────────────────────────────────────────
        public async Task<Category> CreateAsync(Category category)
        {
            category.Id = Guid.NewGuid();

            await _db.Categories.AddAsync(category);
            await _db.SaveChangesAsync();

            return (await GetByIdAsync(category.Id))!;
        }

        // ── UPDATE ────────────────────────────────────────────────────────────
        public async Task<Category?> UpdateAsync(Guid id, Category updatedCategory)
        {
            var existing = await _db.Categories.FindAsync(id);
            if (existing == null) return null;

            existing.Name = updatedCategory.Name;
            existing.Description = updatedCategory.Description;
            existing.ParentCategoryId = updatedCategory.ParentCategoryId;
            existing.IsActive = updatedCategory.IsActive;

            await _db.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        // ── DELETE (soft-delete if referenced by products/sub-categories) ─────
        public async Task<CategoryDeleteResult?> DeleteAsync(Guid id)
        {
            var category = await _db.Categories.FindAsync(id);
            if (category == null) return null;

            var productCount = await GetProductCountAsync(id);
            var subCategoryCount = await GetSubCategoryCountAsync(id);

            if (productCount > 0 || subCategoryCount > 0)
            {
                category.IsActive = false;
                await _db.SaveChangesAsync();

                return new CategoryDeleteResult
                {
                    Category = category,
                    WasSoftDeleted = true,
                    LinkedProductCount = productCount,
                    LinkedSubCategoryCount = subCategoryCount
                };
            }

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();

            return new CategoryDeleteResult
            {
                Category = category,
                WasSoftDeleted = false,
                LinkedProductCount = 0,
                LinkedSubCategoryCount = 0
            };
        }

        public async Task<int> GetProductCountAsync(Guid categoryId)
        {
            return await _db.Products.CountAsync(p => p.CategoryId == categoryId);
        }

        public async Task<int> GetSubCategoryCountAsync(Guid categoryId)
        {
            return await _db.Categories.CountAsync(c => c.ParentCategoryId == categoryId);
        }
    }
}
