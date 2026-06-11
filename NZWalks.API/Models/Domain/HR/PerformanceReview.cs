namespace NZWalks.API.Models.Domain.HR
{
    public class PerformanceReview
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid ReviewerId { get; set; }
        public DateTime ReviewDate { get; set; }
        public string Period { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Strengths { get; set; }
        public string? Improvements { get; set; }
        public string? Goals { get; set; }
        public string? Comments { get; set; }
        public string Status { get; set; } = "Draft";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Employee Employee { get; set; } = null!;
        public Employee Reviewer { get; set; } = null!;
    }
}