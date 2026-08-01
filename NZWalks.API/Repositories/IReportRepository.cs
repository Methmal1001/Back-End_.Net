using NZWalks.API.Models.Domain.Inventory;

namespace NZWalks.API.Repositories
{
    // Per-category counts used by the Category Summary report.
    public class CategorySummaryRow
    {
        public required Category Category { get; set; }
        public int ActiveProductCount { get; set; }
        public int InactiveProductCount { get; set; }
        public int SubCategoryCount { get; set; }
    }

    public interface IReportRepository
    {
        Task<List<Product>> GetProductCatalogAsync(Guid? categoryId, bool? isActive);

        Task<List<Product>> GetPricingDataAsync(Guid? categoryId);

        Task<List<Product>> GetReorderThresholdsAsync(Guid? categoryId);

        Task<List<CategorySummaryRow>> GetCategorySummaryAsync();

        Task<List<Product>> GetInactiveProductsAsync();

        Task<List<DeletedProduct>> GetDeletedProductsAsync(DateTime? fromDate, DateTime? toDate);

        Task<List<ProductAuditLog>> GetProductAuditTrailAsync(Guid? productId, DateTime? fromDate, DateTime? toDate);
    }
}
