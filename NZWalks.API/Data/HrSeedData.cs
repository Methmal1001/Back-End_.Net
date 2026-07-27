using Microsoft.EntityFrameworkCore;
using NZWalks.API.Helpers;
using NZWalks.API.Models.Domain.Inventory;

namespace NZWalks.API.Data
{
    // Idempotent startup seeding for the role/permission catalog. There is no
    // other seeding mechanism in this project, so this runs once on every
    // startup and only inserts what's missing.
    public static class HrSeedData
    {
        private static readonly (string Module, string Action)[] PermissionCatalog =
        {
            ("HR", "ViewLeave"), ("HR", "ManageLeaveTypes"), ("HR", "ApplyLeave"),
            ("HR", "ApproveLeave"), ("HR", "ManageLeaveBalances"),
            ("HR", "ViewAttendance"), ("HR", "MarkAttendance"), ("HR", "ApproveAttendance"),
            ("HR", "ViewOvertime"), ("HR", "ApplyOvertime"), ("HR", "ApproveOvertime"),
            ("HR", "ViewPayroll"), ("HR", "ProcessPayroll"),
            ("HR", "ViewPerformance"), ("HR", "CreatePerformanceReview"),
            ("HR", "ViewDepartment"), ("HR", "CreateDepartment"), ("HR", "UpdateDepartment"),
            ("HR", "ViewJobPosition"), ("HR", "CreateJobPosition"), ("HR", "UpdateJobPosition"),
            ("HR", "ViewEmployee"), ("HR", "CreateEmployee"), ("HR", "UpdateEmployee"), ("HR", "DeleteEmployee"),
            ("HR", "UploadDocument"), ("HR", "DeleteDocument"),
            ("Users", "View"), ("Users", "Create"), ("Users", "Update"), ("Users", "Delete"),
            ("Roles", "View"), ("Roles", "Create"), ("Roles", "AssignPermissions"), ("Roles", "Update"), ("Roles", "Delete"),
            ("Permissions", "View"), ("Permissions", "Create"),
            ("System", "ViewActivityLogs"),
            ("Products", "View"), ("Products", "Create"), ("Products", "Update"), ("Products", "Delete"), ("Products", "ManageCategories"),
        };

        private static readonly string[] HrManagementModules = { "HR", "Users", "Roles", "Permissions" };

        private static readonly string[] EmployeeDefaultPermissions =
        {
            "HR.MarkAttendance", "HR.ViewAttendance",
            "HR.ApplyLeave", "HR.ViewLeave",
            "HR.ApplyOvertime", "HR.ViewOvertime",
        };

        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var inv = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

            var permissions = await EnsurePermissionsAsync(inv);
            var roles = await EnsureRolesAsync(inv);

            var allPermissionIds = permissions.Values.Select(p => p.Id).ToList();
            var hrPermissionIds = permissions
                .Where(kv => HrManagementModules.Contains(kv.Value.Module))
                .Select(kv => kv.Value.Id)
                .ToList();
            var employeePermissionIds = permissions
                .Where(kv => EmployeeDefaultPermissions.Contains($"{kv.Value.Module}.{kv.Value.Action}"))
                .Select(kv => kv.Value.Id)
                .ToList();

            await EnsureRolePermissionsAsync(inv, roles[HrTierRoles.SuperAdmin].Id, allPermissionIds);
            await EnsureRolePermissionsAsync(inv, roles[HrTierRoles.Ceo].Id, allPermissionIds);
            await EnsureRolePermissionsAsync(inv, roles[HrTierRoles.HrAdmin].Id, hrPermissionIds);
            await EnsureRolePermissionsAsync(inv, roles[HrTierRoles.HrAssistant].Id, hrPermissionIds);
            await EnsureRolePermissionsAsync(inv, roles[HrTierRoles.Employee].Id, employeePermissionIds);
        }

        private static async Task<Dictionary<string, Permission>> EnsurePermissionsAsync(InventoryDbContext inv)
        {
            var existing = await inv.Permissions.ToListAsync();

            foreach (var (module, action) in PermissionCatalog)
            {
                if (existing.Any(p => p.Module == module && p.Action == action)) continue;

                var created = new Permission { Module = module, Action = action };
                inv.Permissions.Add(created);
                existing.Add(created);
            }

            await inv.SaveChangesAsync();

            // The Permissions table may already contain duplicate (Module, Action) rows
            // from manual/legacy data entry — keep the first one for each key.
            return existing
                .GroupBy(p => $"{p.Module}.{p.Action}")
                .ToDictionary(g => g.Key, g => g.First());
        }

        private static async Task<Dictionary<string, Role>> EnsureRolesAsync(InventoryDbContext inv)
        {
            var names = new[]
            {
                HrTierRoles.SuperAdmin, HrTierRoles.Ceo, HrTierRoles.HrAdmin,
                HrTierRoles.HrAssistant, HrTierRoles.Employee
            };

            var existing = await inv.Roles.ToListAsync();

            foreach (var name in names)
            {
                if (existing.Any(r => r.Name == name)) continue;

                var created = new Role { Name = name };
                inv.Roles.Add(created);
                existing.Add(created);
            }

            await inv.SaveChangesAsync();

            return existing
                .GroupBy(r => r.Name)
                .ToDictionary(g => g.Key, g => g.First());
        }

        private static async Task EnsureRolePermissionsAsync(InventoryDbContext inv, Guid roleId, List<Guid> permissionIds)
        {
            var existingIds = await inv.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            var missing = permissionIds.Except(existingIds);
            foreach (var permissionId in missing)
                inv.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });

            await inv.SaveChangesAsync();
        }
    }
}
