using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NZWalks.API.Controllers;
using NZWalks.API.Helpers;
using NZWalks.API.Models.Domain.Inventory;
using NZWalks.API.Models.DTO.HR;
using NZWalks.API.Repositories.HR;
using NZWalks.API.Services;

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

        // ── POST api/hr/auth/register ─────────────────────────────────────────
        // No [Authorize] here — open so the first admin user can be created.
        // After your first user is created, you can add [Authorize] + [RequirePermission("Auth","Register")] back.
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
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

                var existing = await _authRepo.GetUserByEmailAsync(dto.Email);
                if (existing != null)
                    return Conflict(new CommonApiResponse<object>
                    {
                        StatusCode = 409,
                        IsSuccess = false,
                        Message = "Email already registered.",
                        Data = null
                    });

                var user = new AppUser
                {
                    Name = dto.Name.Trim(),
                    Email = dto.Email.Trim(),
                    RoleId = dto.RoleId,
                    IsActive = true
                };

                var created = await _authRepo.RegisterAsync(user, dto.Password);

                return Ok(new CommonApiResponse<object>
                {
                    StatusCode = 200,
                    IsSuccess = true,
                    Message = "User registered successfully.",
                    Data = new { created.Id, created.Name, created.Email, created.RoleId }
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

        // ── POST api/hr/auth/login ────────────────────────────────────────────
        [HttpPost("login")]
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

                var user = await _authRepo.GetUserByEmailAsync(dto.Email);
                if (user == null || !PasswordHelper.Verify(dto.Password, user.PasswordHash))
                    return Unauthorized(new CommonApiResponse<object>
                    {
                        StatusCode = 401,
                        IsSuccess = false,
                        Message = "Invalid email or password.",
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

        // ── POST api/hr/auth/logout ───────────────────────────────────────────
        [HttpPost("logout")]
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

        // ── GET api/hr/roles ──────────────────────────────────────────────────
        [HttpGet]
        [RequirePermission("Roles", "View")]
        public async Task<IActionResult> GetAll()
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

        // ── POST api/hr/roles ─────────────────────────────────────────────────
        [HttpPost]
        [RequirePermission("Roles", "Create")]
        public async Task<IActionResult> Create([FromBody] CreateRoleRequestDto dto)
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

        // ── POST api/hr/roles/assign-permissions ──────────────────────────────
        [HttpPost("assign-permissions")]
        [RequirePermission("Roles", "AssignPermissions")]
        public async Task<IActionResult> AssignPermissions([FromBody] AssignPermissionsRequestDto dto)
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

        // ── GET api/hr/permissions ────────────────────────────────────────────
        [HttpGet]
        [RequirePermission("Permissions", "View")]
        public async Task<IActionResult> GetAll()
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

        // ── POST api/hr/permissions ───────────────────────────────────────────
        [HttpPost]
        [RequirePermission("Permissions", "Create")]
        public async Task<IActionResult> Create([FromBody] CreatePermissionRequestDto dto)
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
}