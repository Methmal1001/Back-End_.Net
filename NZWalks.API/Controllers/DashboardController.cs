using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NZWalks.API.Helpers;
using NZWalks.API.Models.DTO.HR;
using NZWalks.API.Repositories.HR;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace NZWalks.API.Controllers.HR
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Dashboard Controller
    // ═══════════════════════════════════════════════════════════════════════════
    //
    // HR-tier roles (Super Admin, CEO, HR Admin, HR Assistant) get an org-wide
    // summary; everyone else gets a personal self-service summary scoped to
    // their own Employee record.

    [Route("api/hr/dashboard")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IAuthRepository _authRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly ILeaveRepository _leaveRepo;
        private readonly IOvertimeRepository _overtimeRepo;
        private readonly IPayrollRepository _payrollRepo;

        public DashboardController(
            IAuthRepository authRepo,
            IEmployeeRepository employeeRepo,
            IAttendanceRepository attendanceRepo,
            ILeaveRepository leaveRepo,
            IOvertimeRepository overtimeRepo,
            IPayrollRepository payrollRepo)
        {
            _authRepo = authRepo;
            _employeeRepo = employeeRepo;
            _attendanceRepo = attendanceRepo;
            _leaveRepo = leaveRepo;
            _overtimeRepo = overtimeRepo;
            _payrollRepo = payrollRepo;
        }

        [HttpGet("Summary")]
        public async Task<IActionResult> Summary()
        {
            if (HrTierRoles.IsHrManagement(User))
                return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Dashboard retrieved.", Data = await BuildOrgSummaryAsync() });

            var callerEmployeeId = await GetCallerEmployeeIdAsync();
            if (callerEmployeeId == null)
                return StatusCode(403, new CommonApiResponse<object> { StatusCode = 403, IsSuccess = false, Message = "No linked employee record for this account.", Data = null });

            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Dashboard retrieved.", Data = await BuildMyDashboardAsync(callerEmployeeId.Value) });
        }

        private async Task<DashboardSummaryDto> BuildOrgSummaryAsync()
        {
            var (employees, _) = await _employeeRepo.GetAllAsync(null, null, "Active", null, false, 1, int.MaxValue);
            var today = DateTime.UtcNow.Date;
            var todaysAttendance = await _attendanceRepo.GetAttendancesAsync(null, today, today);
            var pendingAttendance = await _attendanceRepo.GetAllPendingApprovalsAsync();
            var pendingLeave = await _leaveRepo.GetLeaveRequestsAsync(null, "Pending");
            var pendingOvertime = await _overtimeRepo.GetAllPendingApprovalsAsync();
            var upcomingLeave = await _leaveRepo.GetLeaveRequestsAsync(null, "Approved");
            var payrolls = await _payrollRepo.GetPayrollsAsync(null, today.Month, today.Year);

            var presentToday = todaysAttendance.Count(a => a.Status == "Present");

            return new DashboardSummaryDto
            {
                TotalActiveEmployees = employees.Count,
                HeadcountByDepartment = employees
                    .GroupBy(e => e.Department?.Name ?? "Unassigned")
                    .Select(g => new DepartmentHeadcountDto { DepartmentName = g.Key, Count = g.Count() })
                    .ToList(),
                PresentToday = presentToday,
                AbsentToday = employees.Count - presentToday,
                PendingAttendanceApprovals = pendingAttendance.Count,
                PendingLeaveApprovals = pendingLeave.Count,
                PendingOvertimeApprovals = pendingOvertime.Count,
                UpcomingLeave = upcomingLeave
                    .Where(lr => lr.StartDate.Date >= today && lr.StartDate.Date <= today.AddDays(7))
                    .Select(MapLeaveRequestResponse)
                    .ToList(),
                PayrollStatusThisMonth = payrolls
                    .GroupBy(p => p.Status)
                    .Select(g => new PayrollStatusCountDto { Status = g.Key, Count = g.Count() })
                    .ToList()
            };
        }

        private async Task<MyDashboardDto> BuildMyDashboardAsync(Guid employeeId)
        {
            var employee = await _employeeRepo.GetByIdAsync(employeeId);
            var today = DateTime.UtcNow.Date;
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var todaysAttendance = await _attendanceRepo.GetAttendancesAsync(employeeId, today, today);
            var monthAttendance = await _attendanceRepo.GetAttendancesAsync(employeeId, monthStart, today);
            var leaveBalances = await _leaveRepo.GetLeaveBalancesByEmployeeAsync(employeeId, today.Year);
            var pendingLeave = await _leaveRepo.GetLeaveRequestsAsync(employeeId, "Pending");
            var pendingOvertime = await _overtimeRepo.GetOvertimeRequestsAsync(employeeId, "Pending");

            return new MyDashboardDto
            {
                EmployeeId = employeeId,
                FullName = employee != null ? $"{employee.FirstName} {employee.LastName}" : string.Empty,
                JobTitle = employee?.JobPosition?.Title ?? string.Empty,
                DepartmentName = employee?.Department?.Name ?? string.Empty,
                ManagerName = employee?.Manager != null ? $"{employee.Manager.FirstName} {employee.Manager.LastName}" : null,
                TodayAttendanceStatus = todaysAttendance.FirstOrDefault()?.Status,
                WorkedDaysThisMonth = monthAttendance.Count(a => a.Status == "Present"),
                LeaveBalances = leaveBalances.Select(b => new LeaveBalanceResponseDto
                {
                    EmployeeId = b.EmployeeId,
                    EmployeeName = employee != null ? $"{employee.FirstName} {employee.LastName}" : string.Empty,
                    LeaveTypeName = b.LeaveType?.Name ?? string.Empty,
                    Year = b.Year,
                    TotalDays = b.TotalDays,
                    UsedDays = b.UsedDays,
                    RemainingDays = b.RemainingDays
                }).ToList(),
                PendingLeaveRequests = pendingLeave.Select(MapLeaveRequestResponse).ToList(),
                PendingOvertimeRequests = pendingOvertime.Select(MapOvertimeResponse).ToList()
            };
        }

        private async Task<Guid?> GetCallerEmployeeIdAsync()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId)) return null;

            var employee = await _authRepo.GetEmployeeLinkAsync(userId);
            return employee?.Id;
        }

        private static LeaveRequestResponseDto MapLeaveRequestResponse(Models.Domain.HR.LeaveRequest lr) => new()
        {
            Id = lr.Id,
            EmployeeId = lr.EmployeeId,
            EmployeeName = lr.Employee != null ? $"{lr.Employee.FirstName} {lr.Employee.LastName}" : string.Empty,
            LeaveTypeId = lr.LeaveTypeId,
            LeaveTypeName = lr.LeaveType?.Name ?? string.Empty,
            StartDate = lr.StartDate,
            EndDate = lr.EndDate,
            TotalDays = lr.TotalDays,
            Reason = lr.Reason,
            Status = lr.Status,
            ApprovedById = lr.ApprovedById,
            ApprovalNote = lr.ApprovalNote,
            ApprovedAt = lr.ApprovedAt,
            CreatedAt = lr.CreatedAt
        };

        private static OvertimeResponseDto MapOvertimeResponse(Models.Domain.HR.OvertimeRequest o) => new()
        {
            Id = o.Id,
            EmployeeId = o.EmployeeId,
            EmployeeName = o.Employee != null ? $"{o.Employee.FirstName} {o.Employee.LastName}" : string.Empty,
            Date = o.Date,
            Hours = o.Hours,
            Reason = o.Reason,
            ApprovalStatus = o.ApprovalStatus,
            ApprovedByName = o.ApprovedByEmployee != null ? $"{o.ApprovedByEmployee.FirstName} {o.ApprovedByEmployee.LastName}" : null,
            ApprovedAt = o.ApprovedAt,
            RejectionReason = o.RejectionReason,
            CreatedAt = o.CreatedAt
        };
    }
}
