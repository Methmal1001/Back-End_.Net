using Microsoft.EntityFrameworkCore;
using NZWalks.API.Data;
using NZWalks.API.Helpers;
using NZWalks.API.Models.Domain.HR;
using NZWalks.API.Models.Domain.Inventory;

namespace NZWalks.API.Repositories.HR
{
    public interface IAuthRepository
    {
        Task<AppUser?> GetUserByEmailAsync(string email);
        Task<AppUser> RegisterAsync(AppUser user, string rawPassword);
        Task<List<AppUser>> GetAllUsersAsync();
        Task<AppUser?> GetUserByIdAsync(Guid id);
        Task<AppUser?> UpdateUserAsync(Guid id, AppUser user);
        Task<bool> DeleteUserAsync(Guid id);
        Task<List<string>> GetUserPermissionsAsync(Guid userId);
        Task SaveRefreshTokenAsync(Guid appUserId, string token, DateTime expiresAt);
        Task<HrRefreshToken?> GetRefreshTokenAsync(string token);
        Task RevokeRefreshTokenAsync(string token);
        Task<List<Role>> GetAllRolesAsync();
        Task<Role?> GetRoleByIdAsync(Guid id);
        Task<Role> CreateRoleAsync(Role role);
        Task AssignPermissionsToRoleAsync(Guid roleId, List<Guid> permissionIds);
        Task<List<Permission>> GetAllPermissionsAsync();
        Task<Permission> CreatePermissionAsync(Permission permission);
    }

    public class AuthRepository : IAuthRepository
    {
        private readonly HrDbContext _hr;
        private readonly InventoryDbContext _inv;

        public AuthRepository(HrDbContext hr, InventoryDbContext inv)
        {
            _hr = hr;
            _inv = inv;
        }

        public async Task<AppUser?> GetUserByEmailAsync(string email)
            => await _hr.Users
                .Include(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Email == email.ToLower().Trim());

        public async Task<AppUser> RegisterAsync(AppUser user, string rawPassword)
        {
            user.Email = user.Email.ToLower().Trim();
            user.PasswordHash = PasswordHelper.Hash(rawPassword);
            _inv.Users.Add(user);
            await _inv.SaveChangesAsync();
            return user;
        }

        public async Task<List<AppUser>> GetAllUsersAsync()
            => await _hr.Users.Include(u => u.Role).ToListAsync();

        public async Task<AppUser?> GetUserByIdAsync(Guid id)
            => await _hr.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);

        public async Task<AppUser?> UpdateUserAsync(Guid id, AppUser updated)
        {
            var user = await _inv.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return null;

            user.Name = updated.Name.Trim();
            user.Email = updated.Email.ToLower().Trim();
            user.RoleId = updated.RoleId;
            user.IsActive = updated.IsActive;

            await _inv.SaveChangesAsync();
            return await GetUserByIdAsync(id);
        }

        public async Task<bool> DeleteUserAsync(Guid id)
        {
            var user = await _inv.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return false;

            user.IsActive = false;
            await _inv.SaveChangesAsync();
            return true;
        }

        public async Task<List<string>> GetUserPermissionsAsync(Guid userId)
        {
            var user = await _hr.Users
                .Include(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.Role?.RolePermissions == null) return new();

            return user.Role.RolePermissions
                .Select(rp => $"{rp.Permission.Module}.{rp.Permission.Action}")
                .ToList();
        }

        public async Task SaveRefreshTokenAsync(Guid appUserId, string token, DateTime expiresAt)
        {
            _hr.HrRefreshTokens.Add(new HrRefreshToken
            {
                AppUserId = appUserId,
                Token = token,
                ExpiresAt = expiresAt
            });
            await _hr.SaveChangesAsync();
        }

        public async Task<HrRefreshToken?> GetRefreshTokenAsync(string token)
            => await _hr.HrRefreshTokens
                .FirstOrDefaultAsync(r => r.Token == token && !r.IsRevoked);

        public async Task RevokeRefreshTokenAsync(string token)
        {
            var rt = await _hr.HrRefreshTokens.FirstOrDefaultAsync(r => r.Token == token);
            if (rt != null) { rt.IsRevoked = true; await _hr.SaveChangesAsync(); }
        }

        public async Task<List<Role>> GetAllRolesAsync()
            => await _hr.Roles
                .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
                .ToListAsync();

        public async Task<Role?> GetRoleByIdAsync(Guid id)
            => await _hr.Roles
                .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == id);

        public async Task<Role> CreateRoleAsync(Role role)
        {
            _inv.Roles.Add(role);
            await _inv.SaveChangesAsync();
            return role;
        }

        public async Task AssignPermissionsToRoleAsync(Guid roleId, List<Guid> permissionIds)
        {
            var existing = _inv.RolePermissions.Where(rp => rp.RoleId == roleId);
            _inv.RolePermissions.RemoveRange(existing);
            await _inv.RolePermissions.AddRangeAsync(
                permissionIds.Select(pid => new RolePermission { RoleId = roleId, PermissionId = pid }));
            await _inv.SaveChangesAsync();
        }

        public async Task<List<Permission>> GetAllPermissionsAsync()
            => await _hr.Permissions.ToListAsync();

        public async Task<Permission> CreatePermissionAsync(Permission permission)
        {
            _inv.Permissions.Add(permission);
            await _inv.SaveChangesAsync();
            return permission;
        }
    }
}