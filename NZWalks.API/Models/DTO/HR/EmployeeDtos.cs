using System.ComponentModel.DataAnnotations;

namespace NZWalks.API.Models.DTO.HR
{
    public class CreateDepartmentRequestDto
    {
        [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        [MaxLength(20)] public string? Code { get; set; }
        public Guid? ManagerId { get; set; }
    }

    public class UpdateDepartmentRequestDto : CreateDepartmentRequestDto
    {
        [Required] public Guid Id { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class DepartmentResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Code { get; set; }
        public Guid? ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public bool IsActive { get; set; }
        public int EmployeeCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateJobPositionRequestDto
    {
        [Required, MaxLength(150)] public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        [Required] public Guid DepartmentId { get; set; }
        [MaxLength(50)] public string? Level { get; set; }
        [Range(0, double.MaxValue)] public decimal MinSalary { get; set; }
        [Range(0, double.MaxValue)] public decimal MaxSalary { get; set; }
    }

    public class UpdateJobPositionRequestDto : CreateJobPositionRequestDto
    {
        [Required] public Guid Id { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class JobPositionResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string? Level { get; set; }
        public decimal MinSalary { get; set; }
        public decimal MaxSalary { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateEmployeeRequestDto
    {
        [Required, MaxLength(50)] public string FirstName { get; set; } = string.Empty;
        [Required, MaxLength(50)] public string LastName { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Phone] public string? Phone { get; set; }
        public string? NationalId { get; set; }
        [Required] public DateTime DateOfBirth { get; set; }
        [Required] public string Gender { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        [Required] public Guid DepartmentId { get; set; }
        [Required] public Guid JobPositionId { get; set; }
        public Guid? ManagerId { get; set; }
        [Required] public DateTime JoiningDate { get; set; }
        public string EmploymentType { get; set; } = "FullTime";
        [Range(0, double.MaxValue)] public decimal BasicSalary { get; set; }
        public string? BankAccountNo { get; set; }
        public string? BankName { get; set; }
        public Guid? AppUserId { get; set; }
    }

    public class UpdateEmployeeRequestDto : CreateEmployeeRequestDto
    {
        [Required] public Guid Id { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime? TerminationDate { get; set; }
    }

    public class EmployeeResponseDto
    {
        public Guid Id { get; set; }
        public string EmployeeNo { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? NationalId { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public Guid JobPositionId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public Guid? ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public DateTime JoiningDate { get; set; }
        public DateTime? TerminationDate { get; set; }
        public string EmploymentType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal BasicSalary { get; set; }
        public string? BankAccountNo { get; set; }
        public string? BankName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class EmployeeListResponseDto
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public List<EmployeeResponseDto> Employees { get; set; } = new();
    }
}