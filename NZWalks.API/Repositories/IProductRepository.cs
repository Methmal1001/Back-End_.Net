using NZWalks.API.Models.Domain.Inventory;
using NZWalks.API.Models.DTO.Product;

namespace NZWalks.API.Repositories
{
    public interface IProductRepository
    {
        Task<(List<Product> products, int totalCount)> GetAllAsync(
            string? searchKeyword,
            Guid? categoryId,
            bool? isActive,
            string? sortBy,
            bool isDescending,
            int page,
            int pageSize);

        Task<Product?> GetByIdAsync(Guid id);

        Task<Product?> GetBySkuAsync(string sku);

        Task<Product> CreateAsync(Product product, Guid userId, string userName);

        Task<Product?> UpdateAsync(Guid id, Product updatedProduct, Guid userId, string userName);

        Task<DeletedProduct?> DeleteAsync(Guid id, string? deletionReason, Guid userId, string userName);

        Task<List<DeletedProduct>> GetDeletedProductsAsync();

        Task<DeletedProduct?> GetDeletedProductByIdAsync(Guid deletedRecordId);
        Task GetAllAsync();

        Task<(List<ProductAuditLog> logs, int totalCount)> GetProductAuditLogsAsync(Guid? productId, int page, int pageSize);
    }
}