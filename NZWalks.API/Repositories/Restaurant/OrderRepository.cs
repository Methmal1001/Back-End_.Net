using Microsoft.EntityFrameworkCore;
using NZWalks.API.Data;
using NZWalks.API.Models.Domain.Restaurant;

namespace NZWalks.API.Repositories.Restaurant
{
    public class OrderRepository : IOrderRepository
    {
        private static readonly OrderStatus[] OpenOrderStatuses =
        {
            OrderStatus.Open, OrderStatus.SentToKitchen, OrderStatus.InProgress,
            OrderStatus.ReadyToServe, OrderStatus.Served, OrderStatus.Billed
        };

        private readonly RestaurantDbContext _db;

        public OrderRepository(RestaurantDbContext db)
        {
            _db = db;
        }

        // ── CREATE ORDER ──────────────────────────────────────────────────────
        public async Task<(Order? order, string? error)> CreateOrderAsync(Guid tableId, Guid? userId, string? userName)
        {
            var table = await _db.DiningTables.FindAsync(tableId);
            if (table == null) return (null, $"Table with ID '{tableId}' was not found.");

            var hasOpenOrder = await _db.Orders.AnyAsync(o => o.TableId == tableId && OpenOrderStatuses.Contains(o.Status));
            if (hasOpenOrder) return (null, "This table already has an open order.");

            var todayCount = await _db.Orders.CountAsync(o => o.OpenedAt.Date == DateTime.UtcNow.Date);
            var orderNumber = $"{DateTime.UtcNow:yyyyMMdd}-{todayCount + 1:D4}";

            var order = new Order
            {
                Id = Guid.NewGuid(),
                TableId = tableId,
                OrderNumber = orderNumber,
                Status = OrderStatus.Open,
                OpenedAt = DateTime.UtcNow,
                CreatedByUserId = userId,
                CreatedByUserName = userName
            };

            table.Status = TableStatus.Occupied;
            table.LastUpdatedByUserId = userId;
            table.LastUpdatedByUserName = userName;
            table.LastUpdatedAt = DateTime.UtcNow;

            await _db.Orders.AddAsync(order);
            await _db.SaveChangesAsync();

            return (await GetByIdAsync(order.Id), null);
        }

        public async Task<Order?> GetByIdAsync(Guid id)
        {
            return await _db.Orders
                .Include(o => o.Table)
                .Include(o => o.Items).ThenInclude(i => i.MenuItem)
                .Include(o => o.Items).ThenInclude(i => i.Modifiers)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<(List<Order> orders, int totalCount)> GetAllAsync(
            OrderStatus? status, Guid? tableId, int page, int pageSize)
        {
            var query = _db.Orders
                .Include(o => o.Table)
                .Include(o => o.Items)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            if (tableId.HasValue)
                query = query.Where(o => o.TableId == tableId.Value);

            var totalCount = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.OpenedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (orders, totalCount);
        }

        // ── ORDER ITEMS ───────────────────────────────────────────────────────
        public async Task<MenuItem?> GetMenuItemForOrderingAsync(Guid menuItemId)
        {
            return await _db.MenuItems
                .Include(i => i.ModifierGroups).ThenInclude(g => g.Options)
                .FirstOrDefaultAsync(i => i.Id == menuItemId);
        }

        public async Task<(OrderItem? item, string? error)> AddItemAsync(
            Guid orderId, MenuItem menuItem, int quantity, string? specialInstructions, List<Guid>? selectedModifierOptionIds)
        {
            var order = await _db.Orders.FindAsync(orderId);
            if (order == null) return (null, $"Order with ID '{orderId}' was not found.");

            if (order.Status is OrderStatus.Billed or OrderStatus.Closed or OrderStatus.Cancelled)
                return (null, $"Cannot add items to an order that is already {order.Status}.");

            var (modifiers, error) = ResolveModifiers(menuItem, selectedModifierOptionIds);
            if (error != null) return (null, error);

            var unitPrice = menuItem.Price + modifiers.Sum(m => m.PriceDelta);

            var item = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                MenuItemId = menuItem.Id,
                Quantity = quantity,
                SpecialInstructions = specialInstructions,
                UnitPrice = unitPrice,
                Status = OrderItemStatus.Pending,
                KitchenStation = menuItem.KitchenStation
            };

            foreach (var modifier in modifiers)
            {
                modifier.Id = Guid.NewGuid();
                modifier.OrderItemId = item.Id;
            }

            await _db.OrderItems.AddAsync(item);
            await _db.OrderItemModifiers.AddRangeAsync(modifiers);
            await _db.SaveChangesAsync();

            return (await GetOrderItemAsync(orderId, item.Id), null);
        }

        public async Task<OrderItem?> GetOrderItemAsync(Guid orderId, Guid itemId)
        {
            return await _db.OrderItems
                .Include(i => i.MenuItem)
                .Include(i => i.Modifiers)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.OrderId == orderId);
        }

        public async Task<(OrderItem? item, string? error)> UpdateItemAsync(
            Guid orderId, Guid itemId, MenuItem menuItem, int quantity, string? specialInstructions, List<Guid>? selectedModifierOptionIds)
        {
            var existing = await _db.OrderItems
                .Include(i => i.Modifiers)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.OrderId == orderId);
            if (existing == null) return (null, $"Order item with ID '{itemId}' was not found on this order.");

            if (existing.Status != OrderItemStatus.Pending)
                return (null, "This item is already being prepared or served and can no longer be modified.");

            var (modifiers, error) = ResolveModifiers(menuItem, selectedModifierOptionIds);
            if (error != null) return (null, error);

            _db.OrderItemModifiers.RemoveRange(existing.Modifiers);

            existing.Quantity = quantity;
            existing.SpecialInstructions = specialInstructions;
            existing.UnitPrice = menuItem.Price + modifiers.Sum(m => m.PriceDelta);

            foreach (var modifier in modifiers)
            {
                modifier.Id = Guid.NewGuid();
                modifier.OrderItemId = existing.Id;
            }

            await _db.OrderItemModifiers.AddRangeAsync(modifiers);
            await _db.SaveChangesAsync();

            return (await GetOrderItemAsync(orderId, itemId), null);
        }

        public async Task<(bool success, string? error)> RemoveItemAsync(Guid orderId, Guid itemId)
        {
            var existing = await _db.OrderItems.FirstOrDefaultAsync(i => i.Id == itemId && i.OrderId == orderId);
            if (existing == null) return (false, $"Order item with ID '{itemId}' was not found on this order.");

            if (existing.Status != OrderItemStatus.Pending)
                return (false, "This item is already being prepared or served and can no longer be removed.");

            _db.OrderItems.Remove(existing);
            await _db.SaveChangesAsync();
            return (true, null);
        }

        // ── SEND / CANCEL ─────────────────────────────────────────────────────
        public async Task<(Order? order, string? error)> SendToKitchenAsync(Guid orderId, Guid? userId, string? userName)
        {
            var order = await _db.Orders.FindAsync(orderId);
            if (order == null) return (null, $"Order with ID '{orderId}' was not found.");

            if (order.Status != OrderStatus.Open)
                return (null, $"Order is not open (current status: {order.Status}); it cannot be sent to the kitchen again.");

            var hasItems = await _db.OrderItems.AnyAsync(i => i.OrderId == orderId && i.Status != OrderItemStatus.Cancelled);
            if (!hasItems) return (null, "Cannot send an empty order to the kitchen.");

            order.Status = OrderStatus.SentToKitchen;
            order.LastUpdatedByUserId = userId;
            order.LastUpdatedByUserName = userName;
            order.LastUpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return (await GetByIdAsync(orderId), null);
        }

        public async Task<(Order? order, string? error)> CancelOrderAsync(Guid orderId, Guid? userId, string? userName)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) return (null, $"Order with ID '{orderId}' was not found.");

            if (order.Status is OrderStatus.Billed or OrderStatus.Closed)
                return (null, "Cannot cancel an order that has already been billed.");

            foreach (var item in order.Items.Where(i => i.Status != OrderItemStatus.Served))
                item.Status = OrderItemStatus.Cancelled;

            order.Status = OrderStatus.Cancelled;
            order.ClosedAt = DateTime.UtcNow;
            order.LastUpdatedByUserId = userId;
            order.LastUpdatedByUserName = userName;
            order.LastUpdatedAt = DateTime.UtcNow;

            var table = await _db.DiningTables.FindAsync(order.TableId);
            if (table != null)
            {
                table.Status = TableStatus.Available;
                table.LastUpdatedByUserId = userId;
                table.LastUpdatedByUserName = userName;
                table.LastUpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return (await GetByIdAsync(orderId), null);
        }

        // ── KITCHEN / SERVING ─────────────────────────────────────────────────
        public async Task<List<Order>> GetReadyToServeAsync()
        {
            return await _db.Orders
                .Include(o => o.Table)
                .Include(o => o.Items).ThenInclude(i => i.MenuItem)
                .Include(o => o.Items).ThenInclude(i => i.Modifiers)
                .Where(o => o.Status == OrderStatus.ReadyToServe)
                .OrderBy(o => o.OpenedAt)
                .ToListAsync();
        }

        public async Task<OrderItem?> GetOrderItemByIdAsync(Guid itemId)
        {
            return await _db.OrderItems
                .Include(i => i.MenuItem)
                .Include(i => i.Modifiers)
                .FirstOrDefaultAsync(i => i.Id == itemId);
        }

        public async Task<(OrderItem? item, string? error)> AdvanceKitchenStatusAsync(Guid itemId, OrderItemStatus newStatus)
        {
            var item = await _db.OrderItems.Include(i => i.Order).FirstOrDefaultAsync(i => i.Id == itemId);
            if (item == null) return (null, $"Order item with ID '{itemId}' was not found.");

            var allowedNext = item.Status switch
            {
                OrderItemStatus.Pending => OrderItemStatus.Preparing,
                OrderItemStatus.Preparing => OrderItemStatus.Ready,
                _ => (OrderItemStatus?)null
            };

            if (allowedNext == null || newStatus != allowedNext)
                return (null, $"Cannot move item from {item.Status} to {newStatus}. Kitchen status can only advance Pending → Preparing → Ready.");

            item.Status = newStatus;

            var order = item.Order;
            if (newStatus == OrderItemStatus.Preparing && order.Status == OrderStatus.SentToKitchen)
            {
                order.Status = OrderStatus.InProgress;
            }
            else if (newStatus == OrderItemStatus.Ready)
            {
                // `item` is already tracked with its new Status by EF's identity map,
                // so this reflects the in-memory change even though it isn't saved yet.
                var siblingItems = await _db.OrderItems.Where(i => i.OrderId == order.Id).ToListAsync();
                var allReady = siblingItems
                    .Where(i => i.Status != OrderItemStatus.Cancelled)
                    .All(i => i.Status is OrderItemStatus.Ready or OrderItemStatus.Served);

                if (allReady) order.Status = OrderStatus.ReadyToServe;
            }

            await _db.SaveChangesAsync();
            return (await GetOrderItemByIdAsync(itemId), null);
        }

        public async Task<(OrderItem? item, string? error)> MarkItemServedAsync(Guid orderId, Guid itemId)
        {
            var item = await _db.OrderItems
                .Include(i => i.Order)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.OrderId == orderId);
            if (item == null) return (null, $"Order item with ID '{itemId}' was not found on this order.");

            if (item.Status != OrderItemStatus.Ready)
                return (null, "Item must be Ready before it can be served.");

            item.Status = OrderItemStatus.Served;

            var order = item.Order;
            var siblingItems = await _db.OrderItems.Where(i => i.OrderId == order.Id).ToListAsync();
            var allServed = siblingItems.All(i => i.Status is OrderItemStatus.Served or OrderItemStatus.Cancelled);

            if (allServed) order.Status = OrderStatus.Served;

            await _db.SaveChangesAsync();
            return (await GetOrderItemAsync(orderId, itemId), null);
        }

        public async Task<List<OrderItem>> GetKitchenTicketItemsAsync(KitchenStation? station)
        {
            var query = _db.OrderItems
                .Include(i => i.Order).ThenInclude(o => o.Table)
                .Include(i => i.MenuItem)
                .Include(i => i.Modifiers)
                .Where(i => i.Status == OrderItemStatus.Pending || i.Status == OrderItemStatus.Preparing)
                .Where(i => i.Order.Status != OrderStatus.Open && i.Order.Status != OrderStatus.Cancelled);

            if (station.HasValue)
                query = query.Where(i => i.KitchenStation == station.Value);

            return await query.OrderBy(i => i.Order.OpenedAt).ToListAsync();
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────
        private static (List<OrderItemModifier> modifiers, string? error) ResolveModifiers(MenuItem menuItem, List<Guid>? selectedOptionIds)
        {
            var modifiers = new List<OrderItemModifier>();
            if (selectedOptionIds == null || selectedOptionIds.Count == 0)
                return (modifiers, null);

            var allOptions = menuItem.ModifierGroups.SelectMany(g => g.Options.Select(o => (group: g, option: o))).ToList();

            foreach (var optionId in selectedOptionIds)
            {
                var match = allOptions.FirstOrDefault(x => x.option.Id == optionId);
                if (match.option == null)
                    return (modifiers, $"Modifier option '{optionId}' is not valid for menu item '{menuItem.Name}'.");

                modifiers.Add(new OrderItemModifier
                {
                    ModifierOptionId = match.option.Id,
                    ModifierGroupName = match.group.Name,
                    ModifierOptionName = match.option.Name,
                    PriceDelta = match.option.PriceDelta
                });
            }

            return (modifiers, null);
        }
    }
}
