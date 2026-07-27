using Microsoft.EntityFrameworkCore;
using NZWalks.API.Data;
using NZWalks.API.Models.Domain.Inventory;

namespace NZWalks.API.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly InventoryDbContext _db;

        public ReportRepository(InventoryDbContext db)
        {
            _db = db;
        }

        public async Task<List<Product>> GetProductCatalogAsync(Guid? categoryId, bool? isActive)
        {
            var query = _db.Products.Include(p => p.Category).AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (isActive.HasValue)
                query = query.Where(p => p.IsActive == isActive.Value);

            return await query.OrderBy(p => p.Name).ToListAsync();
        }

        public async Task<List<Product>> GetPricingDataAsync(Guid? categoryId)
        {
            var query = _db.Products.Include(p => p.Category).AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            return await query.ToListAsync();
        }

        public async Task<List<Product>> GetReorderThresholdsAsync(Guid? categoryId)
        {
            var query = _db.Products.Include(p => p.Category).AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            return await query.OrderBy(p => p.Name).ToListAsync();
        }

        public async Task<List<CategorySummaryRow>> GetCategorySummaryAsync()
        {
            var categories = await _db.Categories
                .Include(c => c.ParentCategory)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var activeCounts = await _db.Products
                .Where(p => p.IsActive)
                .GroupBy(p => p.CategoryId)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count);

            var inactiveCounts = await _db.Products
                .Where(p => !p.IsActive)
                .GroupBy(p => p.CategoryId)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count);

            var subCategoryCounts = await _db.Categories
                .Where(c => c.ParentCategoryId != null)
                .GroupBy(c => c.ParentCategoryId!.Value)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count);

            return categories.Select(c => new CategorySummaryRow
            {
                Category = c,
                ActiveProductCount = activeCounts.GetValueOrDefault(c.Id),
                InactiveProductCount = inactiveCounts.GetValueOrDefault(c.Id),
                SubCategoryCount = subCategoryCounts.GetValueOrDefault(c.Id)
            }).ToList();
        }

        public async Task<List<Product>> GetInactiveProductsAsync()
        {
            return await _db.Products
                .Include(p => p.Category)
                .Where(p => !p.IsActive)
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync();
        }

        public async Task<List<DeletedProduct>> GetDeletedProductsAsync(DateTime? fromDate, DateTime? toDate)
        {
            var query = _db.DeletedProducts.AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(d => d.DeletedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(d => d.DeletedAt <= toDate.Value);

            return await query.OrderByDescending(d => d.DeletedAt).ToListAsync();
        }

        public async Task<List<ProductAuditLog>> GetProductAuditTrailAsync(Guid? productId, DateTime? fromDate, DateTime? toDate)
        {
            var query = _db.ProductAuditLogs.AsQueryable();

            if (productId.HasValue)
                query = query.Where(l => l.ProductId == productId.Value);

            if (fromDate.HasValue)
                query = query.Where(l => l.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(l => l.Timestamp <= toDate.Value);

            return await query.OrderByDescending(l => l.Timestamp).ToListAsync();
        }
    }
}
