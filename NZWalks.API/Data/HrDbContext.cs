using Microsoft.EntityFrameworkCore;
using NZWalks.API.Models.Domain.HR;
using NZWalks.API.Models.Domain.Inventory;

namespace NZWalks.API.Data
{
    public class HrDbContext : DbContext
    {
        public HrDbContext(DbContextOptions<HrDbContext> options) : base(options) { }

        public DbSet<Department> Departments { get; set; }
        public DbSet<JobPosition> JobPositions { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<LeaveType> LeaveTypes { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<LeaveBalance> LeaveBalances { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Payroll> Payrolls { get; set; }
        public DbSet<PerformanceReview> PerformanceReviews { get; set; }
        public DbSet<EmployeeDocument> EmployeeDocuments { get; set; }
        public DbSet<HrRefreshToken> HrRefreshTokens { get; set; }

        // Shared read-only tables (owned by InventoryDbContext)
        public DbSet<AppUser> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Exclude shared tables from HR migrations
            modelBuilder.Entity<AppUser>().ToTable("Users", t => t.ExcludeFromMigrations());
            modelBuilder.Entity<Role>().ToTable("Roles", t => t.ExcludeFromMigrations());
            modelBuilder.Entity<Permission>().ToTable("Permissions", t => t.ExcludeFromMigrations());
            modelBuilder.Entity<RolePermission>(e =>
            {
                e.ToTable("RolePermissions", t => t.ExcludeFromMigrations());
                e.HasKey(rp => new { rp.RoleId, rp.PermissionId });
            });

            modelBuilder.Entity<Department>(e =>
            {
                e.HasKey(d => d.Id);
                e.Property(d => d.Name).HasMaxLength(150).IsRequired();
                e.Property(d => d.Code).HasMaxLength(20);
                e.HasOne(d => d.Manager).WithMany()
                    .HasForeignKey(d => d.ManagerId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<JobPosition>(e =>
            {
                e.HasKey(j => j.Id);
                e.Property(j => j.Title).HasMaxLength(150).IsRequired();
                e.Property(j => j.MinSalary).HasColumnType("decimal(18,2)");
                e.Property(j => j.MaxSalary).HasColumnType("decimal(18,2)");
                e.HasOne(j => j.Department).WithMany(d => d.JobPositions)
                    .HasForeignKey(j => j.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Employee>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.EmployeeNo).HasMaxLength(20).IsRequired();
                e.HasIndex(x => x.EmployeeNo).IsUnique();
                e.HasIndex(x => x.Email).IsUnique();
                e.Property(x => x.BasicSalary).HasColumnType("decimal(18,2)");
                e.HasOne(x => x.Department).WithMany(d => d.Employees)
                    .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.JobPosition).WithMany(j => j.Employees)
                    .HasForeignKey(x => x.JobPositionId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Manager).WithMany(m => m.Subordinates)
                    .HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<LeaveBalance>(e =>
            {
                e.HasKey(lb => lb.Id);
                e.HasIndex(lb => new { lb.EmployeeId, lb.LeaveTypeId, lb.Year }).IsUnique();
                e.Ignore(lb => lb.RemainingDays);
                e.HasOne(lb => lb.Employee).WithMany()
                    .HasForeignKey(lb => lb.EmployeeId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(lb => lb.LeaveType).WithMany(lt => lt.LeaveBalances)
                    .HasForeignKey(lb => lb.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<LeaveRequest>(e =>
            {
                e.HasKey(lr => lr.Id);
                e.HasOne(lr => lr.Employee).WithMany(emp => emp.LeaveRequests)
                    .HasForeignKey(lr => lr.EmployeeId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(lr => lr.LeaveType).WithMany(lt => lt.LeaveRequests)
                    .HasForeignKey(lr => lr.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Attendance>(e =>
            {
                e.HasKey(a => a.Id);
                e.HasIndex(a => new { a.EmployeeId, a.Date }).IsUnique();
                e.Property(a => a.ApprovalStatus).HasMaxLength(20);
                e.HasOne(a => a.Employee).WithMany(emp => emp.Attendances)
                    .HasForeignKey(a => a.EmployeeId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(a => a.ApprovedByEmployee).WithMany()
                    .HasForeignKey(a => a.ApprovedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Payroll>(e =>
            {
                e.HasKey(p => p.Id);
                e.HasIndex(p => new { p.EmployeeId, p.Month, p.Year }).IsUnique();
                e.Property(p => p.BasicSalary).HasColumnType("decimal(18,2)");
                e.Property(p => p.Allowances).HasColumnType("decimal(18,2)");
                e.Property(p => p.Bonuses).HasColumnType("decimal(18,2)");
                e.Property(p => p.OvertimePay).HasColumnType("decimal(18,2)");
                e.Property(p => p.Deductions).HasColumnType("decimal(18,2)");
                e.Property(p => p.TaxAmount).HasColumnType("decimal(18,2)");
                e.Property(p => p.NetSalary).HasColumnType("decimal(18,2)");
                e.HasOne(p => p.Employee).WithMany(emp => emp.Payrolls)
                    .HasForeignKey(p => p.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PerformanceReview>(e =>
            {
                e.HasKey(pr => pr.Id);
                e.HasOne(pr => pr.Employee).WithMany(emp => emp.PerformanceReviews)
                    .HasForeignKey(pr => pr.EmployeeId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(pr => pr.Reviewer).WithMany()
                    .HasForeignKey(pr => pr.ReviewerId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<EmployeeDocument>(e =>
            {
                e.HasKey(d => d.Id);
                e.HasOne(d => d.Employee).WithMany(emp => emp.Documents)
                    .HasForeignKey(d => d.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}