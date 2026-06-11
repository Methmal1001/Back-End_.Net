using System.ComponentModel.DataAnnotations;

namespace NZWalks.API.Models.DTO.HR
{
    // ── Leave Type ──────────────────────────────────────────────────────────

    public class CreateLeaveTypeRequestDto
    {
        [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
        [Range(1, 365)] public int MaxDaysPerYear { get; set; }
        public bool IsPaid { get; set; } = true;
    }

    public class LeaveTypeResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MaxDaysPerYear { get; set; }
        public bool IsPaid { get; set; }
        public bool IsActive { get; set; }
    }

    // ── Leave Request ───────────────────────────────────────────────────────

    public class CreateLeaveRequestDto
    {
        [Required] public Guid EmployeeId { get; set; }
        [Required] public Guid LeaveTypeId { get; set; }
        [Required] public DateTime StartDate { get; set; }
        [Required] public DateTime EndDate { get; set; }
        public string? Reason { get; set; }
    }

    public class ApproveLeaveRequestDto
    {
        [Required] public Guid LeaveRequestId { get; set; }
        [Required] public Guid ApprovedById { get; set; }
        [Required] public string Status { get; set; } = string.Empty;
        public string? ApprovalNote { get; set; }
    }

    public class LeaveRequestResponseDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public Guid LeaveTypeId { get; set; }
        public string LeaveTypeName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalDays { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; } = string.Empty;
        public Guid? ApprovedById { get; set; }
        public string? ApprovalNote { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LeaveBalanceResponseDto
    {
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string LeaveTypeName { get; set; } = string.Empty;
        public int Year { get; set; }
        public int TotalDays { get; set; }
        public int UsedDays { get; set; }
        public int RemainingDays { get; set; }
    }

    // ── Attendance ──────────────────────────────────────────────────────────

    public class MarkAttendanceRequestDto
    {
        [Required] public Guid EmployeeId { get; set; }
        [Required] public DateTime Date { get; set; }
        public TimeSpan? CheckInTime { get; set; }
        public TimeSpan? CheckOutTime { get; set; }
        public string Status { get; set; } = "Present";
        public string? Note { get; set; }
    }

    public class AttendanceResponseDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public TimeSpan? CheckInTime { get; set; }
        public TimeSpan? CheckOutTime { get; set; }
        public double? WorkedHours { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ── Payroll ─────────────────────────────────────────────────────────────

    public class GeneratePayrollRequestDto
    {
        [Required] public Guid EmployeeId { get; set; }
        [Required, Range(1, 12)] public int Month { get; set; }
        [Required, Range(2000, 2100)] public int Year { get; set; }
        public decimal Allowances { get; set; }
        public decimal Bonuses { get; set; }
        public decimal OvertimePay { get; set; }
        public decimal Deductions { get; set; }
        public Guid? ProcessedById { get; set; }
    }

    public class PayrollResponseDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal Allowances { get; set; }
        public decimal Bonuses { get; set; }
        public decimal OvertimePay { get; set; }
        public decimal Deductions { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetSalary { get; set; }
        public int WorkingDays { get; set; }
        public int PresentDays { get; set; }
        public int LeaveDays { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ── Performance Review ──────────────────────────────────────────────────

    public class CreatePerformanceReviewRequestDto
    {
        [Required] public Guid EmployeeId { get; set; }
        [Required] public Guid ReviewerId { get; set; }
        [Required] public DateTime ReviewDate { get; set; }
        [Required, MaxLength(50)] public string Period { get; set; } = string.Empty;
        [Range(1, 5)] public int Rating { get; set; }
        public string? Strengths { get; set; }
        public string? Improvements { get; set; }
        public string? Goals { get; set; }
        public string? Comments { get; set; }
    }

    public class PerformanceReviewResponseDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public Guid ReviewerId { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public DateTime ReviewDate { get; set; }
        public string Period { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Strengths { get; set; }
        public string? Improvements { get; set; }
        public string? Goals { get; set; }
        public string? Comments { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // ── Document ────────────────────────────────────────────────────────────

    public class UploadDocumentRequestDto
    {
        [Required] public Guid EmployeeId { get; set; }
        [Required] public string DocumentType { get; set; } = string.Empty;
        [Required] public string FileName { get; set; } = string.Empty;
        [Required] public string FileUrl { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public string? Note { get; set; }
    }

    public class DocumentResponseDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public string? Note { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}