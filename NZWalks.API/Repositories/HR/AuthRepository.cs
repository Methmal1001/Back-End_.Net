using Microsoft.EntityFrameworkCore;
using NZWalks.API.Data;
using NZWalks.API.Helpers;
using NZWalks.API.Models.Domain.HR;
using NZWalks.API.Models.Domain.Inventory;
using NZWalks.API.Models.DTO.HR;

namespace NZWalks.API.Repositories.HR
{
    public enum RoleDeleteResult { NotFound, InUse, Deleted }

    public enum CreateUserWithEmployeeError { None, RoleNotFound, DuplicateEmail, DuplicateEmployeeNo }

    public class CreateUserWithEmployeeResult
    {
        public CreateUserWithEmployeeError Error { get; set; } = CreateUserWithEmployeeError.None;
        public AppUser? User { get; set; }
        public Employee? Employee { get; set; }
    }

    public interface IAuthRepository
    {
        Task<AppUser?> GetUserByEmailAsync(string email);
        Task<AppUser?> GetUserByUsernameOrEmailAsync(string usernameOrEmail);
        Task<List<AppUser>> GetAllUsersAsync();
        Task<AppUser?> GetUserByIdAsync(Guid id);
        Task<AppUser?> UpdateUserAsync(Guid id, AppUser user);
        Task<bool> DeleteUserAsync(Guid id);
        Task<bool> UpdatePasswordAsync(Guid id, string newPasswordHash);
        Task<List<string>> GetUserPermissionsAsync(Guid userId);
        Task SaveRefreshTokenAsync(Guid appUserId, string token, DateTime expiresAt);
        Task<HrRefreshToken?> GetRefreshTokenAsync(string token);
        Task RevokeRefreshTokenAsync(string token);
        Task<List<Role>> GetAllRolesAsync();
        Task<Role?> GetRoleByIdAsync(Guid id);
        Task<Role> CreateRoleAsync(Role role);
        Task<Role?> UpdateRoleAsync(Guid id, string name, string? description);
        Task<RoleDeleteResult> DeleteRoleAsync(Guid id);
        Task AssignPermissionsToRoleAsync(Guid roleId, List<Guid> permissionIds);
        Task<List<Permission>> GetAllPermissionsAsync();
        Task<Permission> CreatePermissionAsync(Permission permission);
        Task<Employee?> GetEmployeeLinkAsync(Guid appUserId);
        Task<Dictionary<Guid, Employee>> GetEmployeeLinksAsync(List<Guid> appUserIds);
        Task<CreateUserWithEmployeeResult> CreateUserWithEmployeeAsync(CreateUserWithEmployeeRequestDto dto);
    }

    public class AuthRepository : IAuthRepository
    {
        private readonly HrDbContext _hr;
        private readonly InventoryDbContext _inv;
        private readonly IEmployeeRepository _employeeRepo;

        public AuthRepository(HrDbContext hr, InventoryDbContext inv, IEmployeeRepository employeeRepo)
        {
            _hr = hr;
            _inv = inv;
            _employeeRepo = employeeRepo;
        }

        public async Task<AppUser?> GetUserByEmailAsync(string email)
            => await _hr.Users
                .Include(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Email == email.ToLower().Trim());

        public async Task<AppUser?> GetUserByUsernameOrEmailAsync(string usernameOrEmail)
        {
            var value = usernameOrEmail.ToLower().Trim();
            return await _hr.Users
                .Include(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u =>
                    (u.Username != null && u.Username.ToLower() == value) ||
                    u.Email.ToLower() == value);
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

        public async Task<bool> UpdatePasswordAsync(Guid id, string newPasswordHash)
        {
            var user = await _inv.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return false;

            user.PasswordHash = newPasswordHash;
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

        public async Task<Role?> UpdateRoleAsync(Guid id, string name, string? description)
        {
            var role = await _inv.Roles.FirstOrDefaultAsync(r => r.Id == id);
            if (role == null) return null;

            role.Name = name.Trim();
            role.Description = description;
            await _inv.SaveChangesAsync();

            return await GetRoleByIdAsync(id);
        }

        public async Task<RoleDeleteResult> DeleteRoleAsync(Guid id)
        {
            var role = await _inv.Roles.FirstOrDefaultAsync(r => r.Id == id);
            if (role == null) return RoleDeleteResult.NotFound;

            var hasUsers = await _inv.Users.AnyAsync(u => u.RoleId == id);
            if (hasUsers) return RoleDeleteResult.InUse;

            var rolePermissions = _inv.RolePermissions.Where(rp => rp.RoleId == id);
            _inv.RolePermissions.RemoveRange(rolePermissions);
            _inv.Roles.Remove(role);
            await _inv.SaveChangesAsync();

            return RoleDeleteResult.Deleted;
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

        public async Task<Employee?> GetEmployeeLinkAsync(Guid appUserId)
            => await _hr.Employees.FirstOrDefaultAsync(e => e.AppUserId == appUserId);

        public async Task<Dictionary<Guid, Employee>> GetEmployeeLinksAsync(List<Guid> appUserIds)
            => await _hr.Employees
                .Where(e => e.AppUserId != null && appUserIds.Contains(e.AppUserId.Value))
                .ToDictionaryAsync(e => e.AppUserId!.Value);

        public async Task<CreateUserWithEmployeeResult> CreateUserWithEmployeeAsync(CreateUserWithEmployeeRequestDto dto)
        {
            var email = dto.Email.ToLower().Trim();

            var roleExists = await _hr.Roles.AnyAsync(r => r.Id == dto.RoleId);
            if (!roleExists)
                return new CreateUserWithEmployeeResult { Error = CreateUserWithEmployeeError.RoleNotFound };

            var emailInUse = await _hr.Users.AnyAsync(u => u.Email == email)
                || await _hr.Employees.AnyAsync(e => e.Email == email);
            if (emailInUse)
                return new CreateUserWithEmployeeResult { Error = CreateUserWithEmployeeError.DuplicateEmail };

            var employeeNo = string.IsNullOrWhiteSpace(dto.EmployeeNo)
                ? await _employeeRepo.GenerateEmployeeNoAsync()
                : dto.EmployeeNo.Trim();

            var employeeNoInUse = await _hr.Employees.AnyAsync(e => e.EmployeeNo == employeeNo);
            if (employeeNoInUse)
                return new CreateUserWithEmployeeResult { Error = CreateUserWithEmployeeError.DuplicateEmployeeNo };

            var nameParts = dto.FullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var firstName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
            var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

            var unassignedDept = await _hr.Departments.FirstOrDefaultAsync(d => d.Name == "Unassigned");
            if (unassignedDept == null)
            {
                unassignedDept = new Department { Name = "Unassigned", IsActive = true };
                _hr.Departments.Add(unassignedDept);
                await _hr.SaveChangesAsync();
            }

            var unassignedPosition = await _hr.JobPositions
                .FirstOrDefaultAsync(j => j.Title == "Unassigned" && j.DepartmentId == unassignedDept.Id);
            if (unassignedPosition == null)
            {
                unassignedPosition = new JobPosition { Title = "Unassigned", DepartmentId = unassignedDept.Id, IsActive = true };
                _hr.JobPositions.Add(unassignedPosition);
                await _hr.SaveChangesAsync();
            }

            using var transaction = await _hr.Database.BeginTransactionAsync();
            try
            {
                var employee = new Employee
                {
                    EmployeeNo = employeeNo,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    Phone = dto.Phone.Trim(),
                    NationalId = dto.Nic.Trim(),
                    DateOfBirth = new DateTime(1900, 1, 1),
                    Gender = "Unspecified",
                    DepartmentId = unassignedDept.Id,
                    JobPositionId = unassignedPosition.Id,
                    JoiningDate = DateTime.UtcNow.Date,
                    Status = "Active"
                };
                _hr.Employees.Add(employee);
                await _hr.SaveChangesAsync();

                var user = new AppUser
                {
                    Name = dto.FullName.Trim(),
                    Username = employeeNo,
                    Email = email,
                    PasswordHash = PasswordHelper.Hash(dto.Nic.Trim()),
                    RoleId = dto.RoleId,
                    IsActive = true
                };
                _hr.Users.Add(user);
                await _hr.SaveChangesAsync();

                employee.AppUserId = user.Id;
                await _hr.SaveChangesAsync();

                await transaction.CommitAsync();

                var fullUser = await GetUserByIdAsync(user.Id);
                return new CreateUserWithEmployeeResult { User = fullUser, Employee = employee };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}