namespace NZWalks.API.Models.Domain.HR
{
    public class EmployeeDocument
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public string? Note { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public Employee Employee { get; set; } = null!;
    }
}