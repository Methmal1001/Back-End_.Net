namespace NZWalks.API.Models.Domain.HR
{
    public class Attendance
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan? CheckInTime { get; set; }
        public TimeSpan? CheckOutTime { get; set; }
        public double? WorkedHours { get; set; }
        public string Status { get; set; } = "Present";
        public string? Note { get; set; }
        public string ApprovalStatus { get; set; } = "Pending";
        public Guid? ApprovedByEmployeeId { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Employee Employee { get; set; } = null!;
        public Employee? ApprovedByEmployee { get; set; }
    }
}