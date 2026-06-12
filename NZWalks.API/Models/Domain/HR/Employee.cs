namespace NZWalks.API.Models.Domain.HR
{
    public class Employee
    {
        public Guid Id { get; set; }
        public string EmployeeNo { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? NationalId { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }

        public Guid DepartmentId { get; set; }
        public Guid JobPositionId { get; set; }
        public Guid? ManagerId { get; set; }
        public DateTime JoiningDate { get; set; }
        public DateTime? TerminationDate { get; set; }
        public string EmploymentType { get; set; } = "FullTime";
        public string Status { get; set; } = "Active";

        public decimal BasicSalary { get; set; }
        public string? BankAccountNo { get; set; }
        public string? BankName { get; set; }

        public Guid? AppUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public Department Department { get; set; } = null!;
        public JobPosition JobPosition { get; set; } = null!;
        public Employee? Manager { get; set; }
        public ICollection<Employee> Subordinates { get; set; } = new List<Employee>();
        public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();
        public ICollection<PerformanceReview> PerformanceReviews { get; set; } = new List<PerformanceReview>();
        public ICollection<EmployeeDocument> Documents { get; set; } = new List<EmployeeDocument>();
    }
}
