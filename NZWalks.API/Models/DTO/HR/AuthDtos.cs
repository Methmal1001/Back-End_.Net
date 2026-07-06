using System.ComponentModel.DataAnnotations;

namespace NZWalks.API.Models.DTO.HR
{
    public class LoginRequestDto
    {
        [Required] public string UsernameOrEmail { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserProfileDto User { get; set; } = null!;
    }

    public class RefreshTokenRequestDto
    {
        [Required] public string RefreshToken { get; set; } = string.Empty;
    }

    public class ChangePasswordRequestDto
    {
        [Required] public string CurrentPassword { get; set; } = string.Empty;
        [Required, MinLength(8)] public string NewPassword { get; set; } = string.Empty;
    }

    public class UserProfileDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string? EmpNo { get; set; }
        public Guid? EmployeeId { get; set; }
        public string Role { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new();
    }

    public class UpdateUserRequestDto
    {
        [Required] public string Name { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public Guid RoleId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UserResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Username { get; set; }
        public Guid? EmployeeId { get; set; }
        public string? EmployeeNo { get; set; }
        public Guid RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateUserWithEmployeeRequestDto
    {
        [Required] public string FullName { get; set; } = string.Empty;
        public string? EmployeeNo { get; set; }
        [Required] public string Nic { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string Phone { get; set; } = string.Empty;
        [Required] public Guid RoleId { get; set; }
    }

    public class CreateRoleRequestDto
    {
        [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class RoleResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> Permissions { get; set; } = new();
    }

    public class AssignPermissionsRequestDto
    {
        [Required] public Guid RoleId { get; set; }
        [Required] public List<Guid> PermissionIds { get; set; } = new();
    }

    public class PermissionResponseDto
    {
        public Guid Id { get; set; }
        public string Module { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class CreatePermissionRequestDto
    {
        [Required, MaxLength(100)] public string Module { get; set; } = string.Empty;
        [Required, MaxLength(100)] public string Action { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}