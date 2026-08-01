using NZWalks.API.Models.Domain.Inventory;

namespace NZWalks.API.Repositories
{
    // Outcome of a delete attempt: the category is soft-deleted (IsActive = false)
    // when it's still referenced by products or sub-categories, otherwise it's
    // removed outright.
    public class CategoryDeleteResult
    {
        public required Category Category { get; set; }
        public bool WasSoftDeleted { get; set; }
        public int LinkedProductCount { get; set; }
        public int LinkedSubCategoryCount { get; set; }
    }

    public interface ICategoryRepository
    {
        Task<(List<Category> categories, Dictionary<Guid, int> subCategoryCounts, Dictionary<Guid, int> productCounts)> GetAllAsync(bool? isActive);

        Task<Category?> GetByIdAsync(Guid id);

        Task<Category> CreateAsync(Category category);

        Task<Category?> UpdateAsync(Guid id, Category updatedCategory);

        Task<CategoryDeleteResult?> DeleteAsync(Guid id);

        Task<int> GetProductCountAsync(Guid categoryId);

        Task<int> GetSubCategoryCountAsync(Guid categoryId);
    }
}
