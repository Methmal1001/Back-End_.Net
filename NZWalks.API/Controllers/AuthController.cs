using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NZWalks.API.Controllers;
using NZWalks.API.Helpers;
using NZWalks.API.Models.Domain.HR;
using NZWalks.API.Models.Domain.Inventory;
using NZWalks.API.Models.DTO.HR;
using NZWalks.API.Repositories.HR;
using NZWalks.API.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace NZWalks.API.Controllers.HR
{
    [Route("api/hr/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepo;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _config;

        public AuthController(IAuthRepository authRepo, ITokenService tokenService, IConfiguration config)
        {
            _authRepo = authRepo;
            _tokenService = tokenService;
            _config = config;
        }

        // ── POST api/hr/auth/ChangePassword ───────────────────────────────────
        [HttpPost("ChangePassword")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object>
                {
                    StatusCode = 400,
                    IsSuccess = false,
                    Message = "Validation failed",
                    Data = ModelState
                });

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new CommonApiResponse<object>
                {
                    StatusCode = 401,
                    IsSuccess = false,
                    Message = "Invalid token.",
                    Data = null
                });

            var user = await _authRepo.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound(new CommonApiResponse<object>
                {
                    StatusCode = 404,
                    IsSuccess = false,
                    Message = "User not found.",
                    Data = null
                });

            if (!PasswordHelper.Verify(dto.CurrentPassword, user.PasswordHash))
                return BadRequest(new CommonApiResponse<object>
                {
                    StatusCode = 400,
                    IsSuccess = false,
                    Message = "Current password is incorrect.",
                    Data = null
                });

            await _authRepo.UpdatePasswordAsync(userId, PasswordHelper.Hash(dto.NewPassword));

            return Ok(new CommonApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = "Password changed successfully.",
                Data = null
            });
        }

        // ── POST api/hr/auth/Login ─────────────────────────────────────────────
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new CommonApiResponse<object>
                    {
                        StatusCode = 400,
                        IsSuccess = false,
                        Message = "Validation failed",
                        Data = ModelState
                    });

                var user = await _authRepo.GetUserByUsernameOrEmailAsync(dto.UsernameOrEmail);
                if (user == null || !PasswordHelper.Verify(dto.Password, user.PasswordHash))
                    return Unauthorized(new CommonApiResponse<object>
                    {
                        StatusCode = 401,
                        IsSuccess = false,
                        Message = "Invalid username/email or password.",
                        Data = null
                    });

                if (!user.IsActive)
                    return Unauthorized(new CommonApiResponse<object>
                    {
                        StatusCode = 401,
                        IsSuccess = false,
                        Message = "Account is inactive.",
                        Data = null
                    });

                var permissions = await _authRepo.GetUserPermissionsAsync(user.Id);
                var accessToken = _tokenService.GenerateAccessToken(user, permissions);
                var refreshToken = _tokenService.GenerateRefreshToken();
                var refreshExpiry = DateTime.UtcNow.AddDays(
                    int.Parse(_config["JwtSettings:RefreshTokenExpiryDays"]!));

                await _authRepo.SaveRefreshTokenAsync(user.Id, refreshToken, refreshExpiry);

                var employeeLink = await _authRepo.GetEmployeeLinkAsync(user.Id);

                return Ok(new CommonApiResponse<LoginResponseDto>
                {
                    StatusCode = 200,
                    IsSuccess = true,
                    Message = "Login successful.",
                    Data = new LoginResponseDto
                    {
                        AccessToken = accessToken,
                        RefreshToken = refreshToken,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(
                            double.Parse(_config["JwtSettings:AccessTokenExpiryMinutes"]!)),
                        User = new UserProfileDto
                        {
                            Id = user.Id,
                            Name = user.Name,
                            Email = user.Email,
                            Username = user.Username,
                            EmployeeId = employeeLink?.Id,
                            Role = user.Role?.Name ?? string.Empty,
                            Permissions = permissions
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CommonApiResponse<object>
                {
                    StatusCode = 500,
                    IsSuccess = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        // ── POST api/hr/auth/Logout ────────────────────────────────────────────
        [HttpPost("Logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto dto)
        {
            await _authRepo.RevokeRefreshTokenAsync(dto.RefreshToken);
            return Ok(new CommonApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = "Logged out successfully.",
                Data = null
            });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Roles Controller
    // ═══════════════════════════════════════════════════════════════════════════

    [Route("api/hr/roles")]
    [ApiController]
    [Authorize]
    public class RolesController : ControllerBase
    {
        private readonly IAuthRepository _authRepo;
        public RolesController(IAuthRepository authRepo) => _authRepo = authRepo;

        // ── GET api/hr/roles/GetRoles ─────────────────────────────────────────
        [HttpGet("GetRoles")]
        [RequirePermission("Roles", "View")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _authRepo.GetAllRolesAsync();
            var result = roles.Select(r => new RoleResponseDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Permissions = r.RolePermissions
                    .Select(rp => $"{rp.Permission.Module}.{rp.Permission.Action}")
                    .ToList()
            });
            return Ok(new CommonApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = "Roles retrieved.",
                Data = result
            });
        }

        // ── POST api/hr/roles/CreateRole ──────────────────────────────────────
        [HttpPost("CreateRole")]
        [RequirePermission("Roles", "Create")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object>
                {
                    StatusCode = 400,
                    IsSuccess = false,
                    Message = "Validation failed",
                    Data = ModelState
                });

            var role = new Role { Name = dto.Name.Trim(), Description = dto.Description };
            var created = await _authRepo.CreateRoleAsync(role);

            return Ok(new CommonApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = "Role created.",
                Data = new RoleResponseDto
                {
                    Id = created.Id,
                    Name = created.Name,
                    Description = created.Description
                }
            });
        }

        // ── POST api/hr/roles/AssignPermissionsToRole ─────────────────────────
        [HttpPost("AssignPermissionsToRole")]
        [RequirePermission("Roles", "AssignPermissions")]
        public async Task<IActionResult> AssignPermissionsToRole([FromBody] AssignPermissionsRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object>
                {
                    StatusCode = 400,
                    IsSuccess = false,
                    Message = "Validation failed",
                    Data = ModelState
                });

            var role = await _authRepo.GetRoleByIdAsync(dto.RoleId);
            if (role == null)
                return NotFound(new CommonApiResponse<object>
                {
                    StatusCode = 404,
                    IsSuccess = false,
                    Message = "Role not found.",
                    Data = null
                });

            await _authRepo.AssignPermissionsToRoleAsync(dto.RoleId, dto.PermissionIds);

            return Ok(new CommonApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = "Permissions assigned successfully.",
                Data = null
            });
        }

        // ── PUT api/hr/roles/UpdateRole?id={id} ───────────────────────────────
        [HttpPut("UpdateRole")]
        [RequireAdminOrPermission("Roles", "Update")]
        public async Task<IActionResult> UpdateRole([FromQuery] Guid id, [FromBody] CreateRoleRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object>
                {
                    StatusCode = 400,
                    IsSuccess = false,
                    Message = "Validation failed",
                    Data = ModelState
                });

            var updated = await _authRepo.UpdateRoleAsync(id, dto.Name, dto.Description);
            if (updated == null)
                return NotFound(new CommonApiResponse<object>
                {
                    StatusCode = 404,
                    IsSuccess = false,
                    Message = "Role not found.",
                    Data = null
                });

            return Ok(new CommonApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = "Role updated.",
                Data = new RoleResponseDto
                {
                    Id = updated.Id,
                    Name = updated.Name,
                    Description = updated.Description,
                    Permissions = updated.RolePermissions
                        .Select(rp => $"{rp.Permission.Module}.{rp.Permission.Action}")
                        .ToList()
                }
            });
        }

        // ── DELETE api/hr/roles/DeleteRole?id={id} ────────────────────────────
        // Fails gracefully (isSuccess=false, 409) if any users are still assigned
        // to this role — reassign or deactivate those users first.
        [HttpDelete("DeleteRole")]
        [RequireAdminOrPermission("Roles", "Delete")]
        public async Task<IActionResult> DeleteRole([FromQuery] Guid id)
        {
            var result = await _authRepo.DeleteRoleAsync(id);

            return result switch
            {
                RoleDeleteResult.NotFound => NotFound(new CommonApiResponse<object>
                {
                    StatusCode = 404,
                    IsSuccess = false,
                    Message = "Role not found.",
                    Data = null
                }),
                RoleDeleteResult.InUse => Conflict(new CommonApiResponse<object>
                {
                    StatusCode = 409,
                    IsSuccess = false,
                    Message = "Cannot delete this role because one or more users are still assigned to it. Reassign those users to a different role first.",
                    Data = null
                }),
                _ => Ok(new CommonApiResponse<object>
                {
                    StatusCode = 200,
                    IsSuccess = true,
                    Message = "Role deleted.",
                    Data = null
                })
            };
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Permissions Controller
    // ═══════════════════════════════════════════════════════════════════════════

    [Route("api/hr/permissions")]
    [ApiController]
    [Authorize]
    public class PermissionsController : ControllerBase
    {
        private readonly IAuthRepository _authRepo;
        public PermissionsController(IAuthRepository authRepo) => _authRepo = authRepo;

        // ── GET api/hr/permissions/GetPermissions ─────────────────────────────
        [HttpGet("GetPermissions")]
        [RequirePermission("Permissions", "View")]
        public async Task<IActionResult> GetPermissions()
        {
            var perms = await _authRepo.GetAllPermissionsAsync();
            var result = perms.Select(p => new PermissionResponseDto
            {
                Id = p.Id,
                Module = p.Module,
                Action = p.Action,
                Description = p.Description
            });
            return Ok(new CommonApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = "Permissions retrieved.",
                Data = result
            });
        }

        // ── POST api/hr/permissions/CreatePermission ──────────────────────────
        [HttpPost("CreatePermission")]
        [RequirePermission("Permissions", "Create")]
        public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object>
                {
                    StatusCode = 400,
                    IsSuccess = false,
                    Message = "Validation failed",
                    Data = ModelState
                });

            var perm = new Permission
            {
                Module = dto.Module.Trim(),
                Action = dto.Action.Trim(),
                Description = dto.Description
            };

            var created = await _authRepo.CreatePermissionAsync(perm);

            return Ok(new CommonApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = "Permission created.",
                Data = new PermissionResponseDto
                {
                    Id = created.Id,
                    Module = created.Module,
                    Action = created.Action,
                    Description = created.Description
                }
            });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Users Controller
    // ═══════════════════════════════════════════════════════════════════════════

    [Route("api/hr/users")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IAuthRepository _authRepo;
        public UsersController(IAuthRepository authRepo) => _authRepo = authRepo;

        // ── GET api/hr/users/GetUsers ─────────────────────────────────────────
        [HttpGet("GetUsers")]
        [RequirePermission("Users", "View")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _authRepo.GetAllUsersAsync();
            var employeeLinks = await _authRepo.GetEmployeeLinksAsync(users.Select(u => u.Id).ToList());
            return Ok(new CommonApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = "Users retrieved.",
                Data = users.Select(u => MapResponse(u, employeeLinks.GetValueOrDefault(u.Id)))
            });
        }

        // ── GET api/hr/users/GetUserById?id={id} ──────────────────────────────
        [HttpGet("GetUserById")]
        [RequirePermission("Users", "View")]
        public async Task<IActionResult> GetUserById([FromQuery] Guid id)
        {
            var user = await _authRepo.GetUserByIdAsync(id);
            if (user == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = "User not found.", Data = null });

            var employee = await _authRepo.GetEmployeeLinkAsync(id);
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "User retrieved.", Data = MapResponse(user, employee) });
        }

        // ── POST api/hr/users/CreateUser ──────────────────────────────────────
        [HttpPost("CreateUser")]
        [RequireAdminOrPermission("Users", "Create")]
        public async Task<IActionResult> CreateUser([FromBody] RegisterRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            var existing = await _authRepo.GetUserByEmailAsync(dto.Email);
            if (existing != null)
                return Conflict(new CommonApiResponse<object> { StatusCode = 409, IsSuccess = false, Message = "Email already registered.", Data = null });

            var user = new AppUser
            {
                Name = dto.Name.Trim(),
                Email = dto.Email.Trim(),
                RoleId = dto.RoleId,
                IsActive = true
            };

            var created = await _authRepo.RegisterAsync(user, dto.Password);
            var full = await _authRepo.GetUserByIdAsync(created.Id);
            var employee = await _authRepo.GetEmployeeLinkAsync(created.Id);

            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "User created.", Data = MapResponse(full!, employee) });
        }

        // ── POST api/hr/users/CreateUserWithEmployee ──────────────────────────
        [HttpPost("CreateUserWithEmployee")]
        [RequireAdminOrPermission("Users", "Create")]
        public async Task<IActionResult> CreateUserWithEmployee([FromBody] CreateUserWithEmployeeRequestDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var result = await _authRepo.CreateUserWithEmployeeAsync(dto);

            switch (result.Error)
            {
                case CreateUserWithEmployeeError.RoleNotFound:
                    ModelState.AddModelError(nameof(dto.RoleId), "Role not found.");
                    return ValidationProblem(ModelState);
                case CreateUserWithEmployeeError.DuplicateEmail:
                    ModelState.AddModelError(nameof(dto.Email), "Email is already in use.");
                    return ValidationProblem(ModelState);
                case CreateUserWithEmployeeError.DuplicateEmployeeNo:
                    ModelState.AddModelError(nameof(dto.EmployeeNo), "Employee number is already in use.");
                    return ValidationProblem(ModelState);
            }

            return Ok(new CommonApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = "User created.",
                Data = MapResponse(result.User!, result.Employee)
            });
        }

        // ── PUT api/hr/users/UpdateUser?id={id} ───────────────────────────────
        [HttpPut("UpdateUser")]
        [RequireAdminOrPermission("Users", "Update")]
        public async Task<IActionResult> UpdateUser([FromQuery] Guid id, [FromBody] UpdateUserRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            var updated = await _authRepo.UpdateUserAsync(id, new AppUser
            {
                Name = dto.Name,
                Email = dto.Email,
                RoleId = dto.RoleId,
                IsActive = dto.IsActive
            });

            if (updated == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = "User not found.", Data = null });

            var employee = await _authRepo.GetEmployeeLinkAsync(id);
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "User updated.", Data = MapResponse(updated, employee) });
        }

        // ── DELETE api/hr/users/DeleteUser?id={id} (soft delete — deactivates) ─
        [HttpDelete("DeleteUser")]
        [RequireAdminOrPermission("Users", "Delete")]
        public async Task<IActionResult> DeleteUser([FromQuery] Guid id)
        {
            var deleted = await _authRepo.DeleteUserAsync(id);
            if (!deleted)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = "User not found.", Data = null });

            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "User deactivated.", Data = null });
        }

        private static UserResponseDto MapResponse(AppUser u, Employee? employee = null) => new()
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            Username = u.Username,
            EmployeeId = employee?.Id,
            EmployeeNo = employee?.EmployeeNo,
            RoleId = u.RoleId,
            RoleName = u.Role?.Name ?? string.Empty,
            IsActive = u.IsActive,
            LastLoginAt = u.LastLoginAt,
            CreatedAt = u.CreatedAt
        };
    }
}