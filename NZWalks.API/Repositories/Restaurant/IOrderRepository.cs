using NZWalks.API.Models.Domain.Restaurant;

namespace NZWalks.API.Repositories.Restaurant
{
    public interface IOrderRepository
    {
        Task<(Order? order, string? error)> CreateOrderAsync(Guid tableId, Guid? userId, string? userName);
        Task<Order?> GetByIdAsync(Guid id);
        Task<(List<Order> orders, int totalCount)> GetAllAsync(OrderStatus? status, Guid? tableId, int page, int pageSize);

        Task<MenuItem?> GetMenuItemForOrderingAsync(Guid menuItemId);
        Task<(OrderItem? item, string? error)> AddItemAsync(
            Guid orderId, MenuItem menuItem, int quantity, string? specialInstructions, List<Guid>? selectedModifierOptionIds);

        Task<OrderItem?> GetOrderItemAsync(Guid orderId, Guid itemId);
        Task<(OrderItem? item, string? error)> UpdateItemAsync(
            Guid orderId, Guid itemId, MenuItem menuItem, int quantity, string? specialInstructions, List<Guid>? selectedModifierOptionIds);
        Task<(bool success, string? error)> RemoveItemAsync(Guid orderId, Guid itemId);

        Task<(Order? order, string? error)> SendToKitchenAsync(Guid orderId, Guid? userId, string? userName);
        Task<(Order? order, string? error)> CancelOrderAsync(Guid orderId, Guid? userId, string? userName);

        Task<List<Order>> GetReadyToServeAsync();
        Task<OrderItem?> GetOrderItemByIdAsync(Guid itemId);
        Task<(OrderItem? item, string? error)> AdvanceKitchenStatusAsync(Guid itemId, OrderItemStatus newStatus);
        Task<(OrderItem? item, string? error)> MarkItemServedAsync(Guid orderId, Guid itemId);
        Task<List<OrderItem>> GetKitchenTicketItemsAsync(KitchenStation? station);
    }
}
