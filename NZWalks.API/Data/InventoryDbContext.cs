using Microsoft.EntityFrameworkCore;
using NZWalks.API.Models.Domain.Inventory;

namespace NZWalks.API.Data
{
    public class InventoryDbContext : DbContext
    {
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

        // Catalog
        public DbSet<Category> Categories { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Product> Products { get; set; }

        // Warehouse
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<WarehouseZone> WarehouseZones { get; set; }
        public DbSet<StockLocation> StockLocations { get; set; }

        // Inventory
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }

        // Purchasing
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
        public DbSet<GoodsReceipt> GoodsReceipts { get; set; }
        public DbSet<GoodsReceiptItem> GoodsReceiptItems { get; set; }

        // Sales
        public DbSet<Customer> Customers { get; set; }
        public DbSet<SalesOrder> SalesOrders { get; set; }
        public DbSet<SalesOrderItem> SalesOrderItems { get; set; }

        // Stock Operations
        public DbSet<StockTransfer> StockTransfers { get; set; }
        public DbSet<StockTransferItem> StockTransferItems { get; set; }
        public DbSet<StockAdjustment> StockAdjustments { get; set; }
        public DbSet<StockAdjustmentItem> StockAdjustmentItems { get; set; }
        public DbSet<StockCount> StockCounts { get; set; }
        public DbSet<StockCountItem> StockCountItems { get; set; }

        // Auth & Audit
        public DbSet<AppUser> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        // ✅ Deleted products archive
        public DbSet<DeletedProduct> DeletedProducts { get; set; }

        // ✅ Product audit trail
        public DbSet<ProductAuditLog> ProductAuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ---- Composite PK ----
            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            // ---- Category (self-referencing) ----
            modelBuilder.Entity<Category>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---- StockTransfer ----
            modelBuilder.Entity<StockTransfer>()
                .HasOne(t => t.FromWarehouse)
                .WithMany()
                .HasForeignKey(t => t.FromWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockTransfer>()
                .HasOne(t => t.ToWarehouse)
                .WithMany()
                .HasForeignKey(t => t.ToWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockTransfer>()
                .HasOne(t => t.CreatedBy)
                .WithMany()
                .HasForeignKey(t => t.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            // ---- StockAdjustment ----
            modelBuilder.Entity<StockAdjustment>()
                .HasOne(a => a.CreatedBy)
                .WithMany()
                .HasForeignKey(a => a.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockAdjustment>()
                .HasOne(a => a.ApprovedBy)
                .WithMany()
                .HasForeignKey(a => a.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockAdjustment>()
                .HasOne(a => a.Warehouse)
                .WithMany()
                .HasForeignKey(a => a.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---- GoodsReceipt ----
            modelBuilder.Entity<GoodsReceipt>()
                .HasOne(g => g.ReceivedBy)
                .WithMany()
                .HasForeignKey(g => g.ReceivedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GoodsReceipt>()
                .HasOne(g => g.Warehouse)
                .WithMany()
                .HasForeignKey(g => g.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GoodsReceipt>()
                .HasOne(g => g.PurchaseOrder)
                .WithMany(p => p.GoodsReceipts)
                .HasForeignKey(g => g.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---- PurchaseOrder ----
            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(p => p.CreatedBy)
                .WithMany()
                .HasForeignKey(p => p.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(p => p.Warehouse)
                .WithMany()
                .HasForeignKey(p => p.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(p => p.Supplier)
                .WithMany(s => s.PurchaseOrders)
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---- PurchaseOrderItem ----
            modelBuilder.Entity<PurchaseOrderItem>()
                .HasOne(p => p.PurchaseOrder)
                .WithMany(o => o.Items)
                .HasForeignKey(p => p.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseOrderItem>()
                .HasOne(p => p.Product)
                .WithMany()
                .HasForeignKey(p => p.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---- SalesOrder ----
            modelBuilder.Entity<SalesOrder>()
                .HasOne(s => s.CreatedBy)
                .WithMany()
                .HasForeignKey(s => s.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalesOrder>()
                .HasOne(s => s.Warehouse)
                .WithMany()
                .HasForeignKey(s => s.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalesOrder>()
                .HasOne(s => s.Customer)
                .WithMany(c => c.SalesOrders)
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---- SalesOrderItem ----
            modelBuilder.Entity<SalesOrderItem>()
                .HasOne(s => s.SalesOrder)
                .WithMany(o => o.Items)
                .HasForeignKey(s => s.SalesOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalesOrderItem>()
                .HasOne(s => s.Product)
                .WithMany()
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---- StockCount ----
            modelBuilder.Entity<StockCount>()
                .HasOne(s => s.CreatedBy)
                .WithMany()
                .HasForeignKey(s => s.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockCount>()
                .HasOne(s => s.Warehouse)
                .WithMany()
                .HasForeignKey(s => s.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---- StockCountItem ----
            modelBuilder.Entity<StockCountItem>()
                .HasOne(s => s.StockCount)
                .WithMany(c => c.Items)
                .HasForeignKey(s => s.StockCountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockCountItem>()
                .HasOne(s => s.Product)
                .WithMany()
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockCountItem>()
                .HasOne(s => s.Location)
                .WithMany()
                .HasForeignKey(s => s.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockCountItem>()
                .HasOne(s => s.CountedBy)
                .WithMany()
                .HasForeignKey(s => s.CountedById)
                .OnDelete(DeleteBehavior.Restrict);

            // ---- StockMovement ----
            modelBuilder.Entity<StockMovement>()
                .HasOne(s => s.CreatedBy)
                .WithMany()
                .HasForeignKey(s => s.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockMovement>()
                .HasOne(s => s.Product)
                .WithMany()
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---- StockAdjustmentItem ----
            modelBuilder.Entity<StockAdjustmentItem>()
                .HasOne(s => s.Adjustment)
                .WithMany(a => a.Items)
                .HasForeignKey(s => s.AdjustmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockAdjustmentItem>()
                .HasOne(s => s.Product)
                .WithMany()
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockAdjustmentItem>()
                .HasOne(s => s.Location)
                .WithMany()
                .HasForeignKey(s => s.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---- StockTransferItem ----
            modelBuilder.Entity<StockTransferItem>()
                .HasOne(s => s.Transfer)
                .WithMany(t => t.Items)
                .HasForeignKey(s => s.TransferId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockTransferItem>()
                .HasOne(s => s.Product)
                .WithMany()
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockTransferItem>()
                .HasOne(s => s.FromLocation)
                .WithMany()
                .HasForeignKey(s => s.FromLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockTransferItem>()
                .HasOne(s => s.ToLocation)
                .WithMany()
                .HasForeignKey(s => s.ToLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---- GoodsReceiptItem ----
            modelBuilder.Entity<GoodsReceiptItem>()
                .HasOne(g => g.GoodsReceipt)
                .WithMany(r => r.Items)
                .HasForeignKey(g => g.GoodsReceiptId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GoodsReceiptItem>()
                .HasOne(g => g.Product)
                .WithMany()
                .HasForeignKey(g => g.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GoodsReceiptItem>()
                .HasOne(g => g.PoItem)
                .WithMany()
                .HasForeignKey(g => g.PoItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GoodsReceiptItem>()
                .HasOne(g => g.Location)
                .WithMany()
                .HasForeignKey(g => g.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---- AuditLog ----
            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---- Inventory ----
            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.Product)
                .WithMany(p => p.Inventories)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.Location)
                .WithMany(l => l.Inventories)
                .HasForeignKey(i => i.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---- Decimal precision ----
            modelBuilder.Entity<Product>()
                .Property(p => p.UnitCost).HasColumnType("decimal(18,4)");
            modelBuilder.Entity<Product>()
                .Property(p => p.UnitPrice).HasColumnType("decimal(18,4)");

            modelBuilder.Entity<Supplier>()
                .Property(s => s.Rating).HasColumnType("decimal(3,2)");

            modelBuilder.Entity<Customer>()
                .Property(c => c.CreditLimit).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<PurchaseOrder>()
                .Property(p => p.Subtotal).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<PurchaseOrder>()
                .Property(p => p.TaxAmount).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<PurchaseOrder>()
                .Property(p => p.ShippingCost).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<PurchaseOrder>()
                .Property(p => p.TotalAmount).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<PurchaseOrderItem>()
                .Property(p => p.UnitCost).HasColumnType("decimal(18,4)");
            modelBuilder.Entity<PurchaseOrderItem>()
                .Property(p => p.LineTotal).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<SalesOrder>()
                .Property(s => s.Subtotal).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<SalesOrder>()
                .Property(s => s.TaxAmount).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<SalesOrder>()
                .Property(s => s.DiscountAmount).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<SalesOrder>()
                .Property(s => s.TotalAmount).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<SalesOrderItem>()
                .Property(s => s.UnitPrice).HasColumnType("decimal(18,4)");
            modelBuilder.Entity<SalesOrderItem>()
                .Property(s => s.DiscountPercent).HasColumnType("decimal(5,2)");
            modelBuilder.Entity<SalesOrderItem>()
                .Property(s => s.LineTotal).HasColumnType("decimal(18,2)");

            // ---- DeletedProduct decimal precision ----
            modelBuilder.Entity<DeletedProduct>()
                .Property(d => d.UnitCost).HasColumnType("decimal(18,4)");
            modelBuilder.Entity<DeletedProduct>()
                .Property(d => d.UnitPrice).HasColumnType("decimal(18,4)");
        }
    }
}