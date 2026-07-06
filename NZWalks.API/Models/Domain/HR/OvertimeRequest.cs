namespace NZWalks.API.Models.Domain.HR
{
    public class OvertimeRequest
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public DateTime Date { get; set; }
        public double Hours { get; set; }
        public string? Reason { get; set; }
        public string ApprovalStatus { get; set; } = "Pending";
        public Guid? ApprovedByEmployeeId { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Employee Employee { get; set; } = null!;
        public Employee? ApprovedByEmployee { get; set; }
    }
}
