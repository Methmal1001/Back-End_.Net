namespace NZWalks.API.Models.DTO.HR
{
    // ── HR-tier org-wide dashboard ──────────────────────────────────────────

    public class DashboardSummaryDto
    {
        public int TotalActiveEmployees { get; set; }
        public List<DepartmentHeadcountDto> HeadcountByDepartment { get; set; } = new();
        public int PresentToday { get; set; }
        public int AbsentToday { get; set; }
        public int PendingAttendanceApprovals { get; set; }
        public int PendingLeaveApprovals { get; set; }
        public int PendingOvertimeApprovals { get; set; }
        public List<LeaveRequestResponseDto> UpcomingLeave { get; set; } = new();
        public List<PayrollStatusCountDto> PayrollStatusThisMonth { get; set; } = new();
    }

    public class DepartmentHeadcountDto
    {
        public string DepartmentName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class PayrollStatusCountDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    // ── Personal (non-HR-tier) dashboard ────────────────────────────────────

    public class MyDashboardDto
    {
        public Guid EmployeeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string? ManagerName { get; set; }
        public string? TodayAttendanceStatus { get; set; }
        public int WorkedDaysThisMonth { get; set; }
        public List<LeaveBalanceResponseDto> LeaveBalances { get; set; } = new();
        public List<LeaveRequestResponseDto> PendingLeaveRequests { get; set; } = new();
        public List<OvertimeResponseDto> PendingOvertimeRequests { get; set; } = new();
    }
}
