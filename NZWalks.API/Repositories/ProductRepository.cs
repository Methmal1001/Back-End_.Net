using Microsoft.EntityFrameworkCore;
using NZWalks.API.Data;
using NZWalks.API.Models.Domain.Inventory;
using NZWalks.API.Models.DTO.Product;
using System.Text.Json;

namespace NZWalks.API.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly InventoryDbContext _db;

        public ProductRepository(InventoryDbContext db)
        {
            _db = db;
        }

        // ── GET ALL (with search, filter, sort, pagination) ──────────────────
        public async Task<(List<Product> products, int totalCount)> GetAllAsync(
            string? searchKeyword,
            Guid? categoryId,
            bool? isActive,
            string? sortBy,
            bool isDescending,
            int page,
            int pageSize)
        {
            var query = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(searchKeyword))
            {
                var kw = searchKeyword.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(kw) ||
                    p.Sku.ToLower().Contains(kw) ||
                    (p.Barcode != null && p.Barcode.ToLower().Contains(kw)) ||
                    (p.Description != null && p.Description.ToLower().Contains(kw)));
            }

            // Filter by category
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            // Filter by active status
            if (isActive.HasValue)
                query = query.Where(p => p.IsActive == isActive.Value);

            // Total count before pagination
            var totalCount = await query.CountAsync();

            // Sort
            query = sortBy?.ToLower() switch
            {
                "name" => isDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                "sku" => isDescending ? query.OrderByDescending(p => p.Sku) : query.OrderBy(p => p.Sku),
                "unitprice" => isDescending ? query.OrderByDescending(p => p.UnitPrice) : query.OrderBy(p => p.UnitPrice),
                "unitcost" => isDescending ? query.OrderByDescending(p => p.UnitCost) : query.OrderBy(p => p.UnitCost),
                "createdat" => isDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
                _ => query.OrderBy(p => p.Name)
            };

            // Pagination
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (products, totalCount);
        }

        // ── GET BY ID ─────────────────────────────────────────────────────────
        public async Task<Product?> GetByIdAsync(Guid id)
        {
            return await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        // ── GET BY SKU ────────────────────────────────────────────────────────
        public async Task<Product?> GetBySkuAsync(string sku)
        {
            return await _db.Products
                .FirstOrDefaultAsync(p => p.Sku.ToLower() == sku.ToLower());
        }

        // ── CREATE ────────────────────────────────────────────────────────────
        public async Task<Product> CreateAsync(Product product, Guid userId, string userName)
        {
            product.Id = Guid.NewGuid();
            product.CreatedAt = DateTime.UtcNow;
            product.CreatedByUserId = userId;
            product.CreatedByUserName = userName;

            await _db.Products.AddAsync(product);
            await _db.SaveChangesAsync();

            await _db.ProductAuditLogs.AddAsync(new ProductAuditLog
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Action = ProductAuditAction.Create,
                UserId = userId,
                UserName = userName,
                Timestamp = DateTime.UtcNow,
                Details = JsonSerializer.Serialize(new { @new = BuildSnapshot(product) })
            });
            await _db.SaveChangesAsync();

            // Reload with navigation properties
            return (await GetByIdAsync(product.Id))!;
        }

        // ── UPDATE ────────────────────────────────────────────────────────────
        public async Task<Product?> UpdateAsync(Guid id, Product updatedProduct, Guid userId, string userName)
        {
            var existing = await _db.Products.FindAsync(id);
            if (existing == null) return null;

            var oldSnapshot = BuildSnapshot(existing);

            existing.Sku = updatedProduct.Sku;
            existing.Name = updatedProduct.Name;
            existing.Description = updatedProduct.Description;
            existing.CategoryId = updatedProduct.CategoryId;
            existing.SupplierId = updatedProduct.SupplierId;
            existing.UnitCost = updatedProduct.UnitCost;
            existing.UnitPrice = updatedProduct.UnitPrice;
            existing.UnitOfMeasure = updatedProduct.UnitOfMeasure;
            existing.ReorderPoint = updatedProduct.ReorderPoint;
            existing.ReorderQuantity = updatedProduct.ReorderQuantity;
            existing.MinStockLevel = updatedProduct.MinStockLevel;
            existing.MaxStockLevel = updatedProduct.MaxStockLevel;
            existing.IsActive = updatedProduct.IsActive;
            existing.Barcode = updatedProduct.Barcode;
            existing.ImageUrl = updatedProduct.ImageUrl;
            existing.UpdatedByUserId = userId;
            existing.UpdatedByUserName = userName;
            existing.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _db.ProductAuditLogs.AddAsync(new ProductAuditLog
            {
                Id = Guid.NewGuid(),
                ProductId = id,
                Action = ProductAuditAction.Update,
                UserId = userId,
                UserName = userName,
                Timestamp = DateTime.UtcNow,
                Details = JsonSerializer.Serialize(new { old = oldSnapshot, @new = BuildSnapshot(existing) })
            });
            await _db.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        // ── DELETE (soft-archive to DeletedProducts) ──────────────────────────
        public async Task<DeletedProduct?> DeleteAsync(Guid id, string? deletionReason, Guid userId, string userName)
        {
            var product = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return null;

            var snapshot = BuildSnapshot(product);

            // Archive snapshot
            var deleted = new DeletedProduct
            {
                Id = Guid.NewGuid(),
                OriginalProductId = product.Id,
                Sku = product.Sku,
                Name = product.Name,
                Description = product.Description,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name ?? string.Empty,
                SupplierId = product.SupplierId,
                SupplierName = product.Supplier?.Name,
                UnitCost = product.UnitCost,
                UnitPrice = product.UnitPrice,
                UnitOfMeasure = product.UnitOfMeasure,
                ReorderPoint = product.ReorderPoint,
                ReorderQuantity = product.ReorderQuantity,
                MinStockLevel = product.MinStockLevel,
                MaxStockLevel = product.MaxStockLevel,
                Barcode = product.Barcode,
                ImageUrl = product.ImageUrl,
                OriginalCreatedAt = product.CreatedAt,
                DeletedByUserId = userId.ToString(),
                DeletedByUserName = userName,
                DeletionReason = deletionReason,
                DeletedAt = DateTime.UtcNow
            };

            await _db.DeletedProducts.AddAsync(deleted);
            _db.Products.Remove(product);

            await _db.ProductAuditLogs.AddAsync(new ProductAuditLog
            {
                Id = Guid.NewGuid(),
                ProductId = id,
                Action = ProductAuditAction.Delete,
                UserId = userId,
                UserName = userName,
                Timestamp = DateTime.UtcNow,
                Details = JsonSerializer.Serialize(new { old = snapshot })
            });

            await _db.SaveChangesAsync();

            return deleted;
        }

        // ── GET ALL DELETED ───────────────────────────────────────────────────
        public async Task<List<DeletedProduct>> GetDeletedProductsAsync()
        {
            return await _db.DeletedProducts
                .OrderByDescending(d => d.DeletedAt)
                .ToListAsync();
        }

        // ── GET SINGLE DELETED RECORD ─────────────────────────────────────────
        public async Task<DeletedProduct?> GetDeletedProductByIdAsync(Guid deletedRecordId)
        {
            return await _db.DeletedProducts
                .FirstOrDefaultAsync(d => d.Id == deletedRecordId);
        }

        // ── AUDIT LOGS (optionally filtered by product, paginated) ────────────
        public async Task<(List<ProductAuditLog> logs, int totalCount)> GetProductAuditLogsAsync(Guid? productId, int page, int pageSize)
        {
            var query = _db.ProductAuditLogs.AsQueryable();

            if (productId.HasValue)
                query = query.Where(l => l.ProductId == productId.Value);

            var totalCount = await query.CountAsync();

            var logs = await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (logs, totalCount);
        }

        public Task GetAllAsync()
        {
            throw new NotImplementedException();
        }

        private static object BuildSnapshot(Product p) => new
        {
            p.Sku,
            p.Name,
            p.Description,
            p.CategoryId,
            p.SupplierId,
            p.UnitCost,
            p.UnitPrice,
            p.UnitOfMeasure,
            p.ReorderPoint,
            p.ReorderQuantity,
            p.MinStockLevel,
            p.MaxStockLevel,
            p.IsActive,
            p.Barcode,
            p.ImageUrl
        };
    }
}
