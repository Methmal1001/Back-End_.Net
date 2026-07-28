using NZWalks.API.Models.Domain.Restaurant;

namespace NZWalks.API.Repositories.Restaurant
{
    public class MenuDeleteOutcome
    {
        public bool WasSoftDeleted { get; set; }
        public int LinkedCount { get; set; }
    }

    public interface IMenuRepository
    {
        Task<List<MenuCategory>> GetCategoriesAsync(bool? isActive);
        Task<MenuCategory?> GetCategoryByIdAsync(Guid id);
        Task<int> GetItemCountForCategoryAsync(Guid categoryId);
        Task<MenuCategory> CreateCategoryAsync(MenuCategory category);
        Task<MenuCategory?> UpdateCategoryAsync(Guid id, MenuCategory updated);
        Task<MenuDeleteOutcome?> DeleteCategoryAsync(Guid id);

        Task<(List<MenuItem> items, int totalCount)> GetItemsAsync(Guid? categoryId, bool? isAvailable, int page, int pageSize);
        Task<MenuItem?> GetItemByIdAsync(Guid id);
        Task<MenuItem> CreateItemAsync(MenuItem item, List<ModifierGroup> modifierGroups);
        Task<MenuItem?> UpdateItemAsync(Guid id, MenuItem updated, List<ModifierGroup>? modifierGroups);
        Task<MenuDeleteOutcome?> DeleteItemAsync(Guid id);
        Task<MenuItem?> SetAvailabilityAsync(Guid id, bool isAvailable, Guid? userId, string? userName);
    }
}
