using Microsoft.EntityFrameworkCore;
using NZWalks.API.Models.Domain.Restaurant;

namespace NZWalks.API.Data
{
    // Fully independent from InventoryDbContext — the restaurant vertical does
    // not reference Product/Category at all, by design.
    public class RestaurantDbContext : DbContext
    {
        public RestaurantDbContext(DbContextOptions<RestaurantDbContext> options) : base(options) { }

        public DbSet<MenuCategory> MenuCategories { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<ModifierGroup> ModifierGroups { get; set; }
        public DbSet<ModifierOption> ModifierOptions { get; set; }
        public DbSet<DiningTable> DiningTables { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<OrderItemModifier> OrderItemModifiers { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MenuItem>()
                .HasOne(m => m.Category)
                .WithMany(c => c.MenuItems)
                .HasForeignKey(m => m.MenuCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ModifierGroup>()
                .HasOne(g => g.MenuItem)
                .WithMany(m => m.ModifierGroups)
                .HasForeignKey(g => g.MenuItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ModifierOption>()
                .HasOne(o => o.ModifierGroup)
                .WithMany(g => g.Options)
                .HasForeignKey(o => o.ModifierGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Table)
                .WithMany()
                .HasForeignKey(o => o.TableId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderItem>()
                .HasOne(i => i.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne(i => i.MenuItem)
                .WithMany()
                .HasForeignKey(i => i.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderItemModifier>()
                .HasOne(m => m.OrderItem)
                .WithMany(i => i.Modifiers)
                .HasForeignKey(m => m.OrderItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Order)
                .WithMany(o => o.Payments)
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MenuItem>().Property(m => m.Price).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<ModifierOption>().Property(o => o.PriceDelta).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<OrderItem>().Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<OrderItemModifier>().Property(m => m.PriceDelta).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Order>().Property(o => o.Subtotal).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Order>().Property(o => o.TaxAmount).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Order>().Property(o => o.ServiceChargeAmount).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Order>().Property(o => o.DiscountAmount).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Order>().Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Payment>().Property(p => p.Amount).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Order>().HasIndex(o => o.OrderNumber).IsUnique();
            modelBuilder.Entity<Order>().HasIndex(o => o.Status);
            modelBuilder.Entity<Order>().HasIndex(o => o.TableId);
            modelBuilder.Entity<OrderItem>().HasIndex(i => i.Status);
            modelBuilder.Entity<OrderItem>().HasIndex(i => i.KitchenStation);
        }
    }
}
