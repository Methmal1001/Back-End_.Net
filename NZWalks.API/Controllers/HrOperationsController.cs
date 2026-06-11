using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NZWalks.API.Controllers;
using NZWalks.API.Helpers;
using NZWalks.API.Models.Domain.HR;
using NZWalks.API.Models.DTO.HR;
using NZWalks.API.Repositories.HR;

namespace NZWalks.API.Controllers.HR
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Leave Controller
    // ═══════════════════════════════════════════════════════════════════════════

    [Route("api/hr/leave")]
    [ApiController]
    [Authorize]
    public class LeaveController : ControllerBase
    {
        private readonly ILeaveRepository _repo;
        public LeaveController(ILeaveRepository repo) => _repo = repo;

        // ── Leave Types ───────────────────────────────────────────────────────

        [HttpGet("types")]
        [RequirePermission("HR", "ViewLeave")]
        public async Task<IActionResult> GetLeaveTypes()
        {
            var types = await _repo.GetAllLeaveTypesAsync();
            var result = types.Select(t => new LeaveTypeResponseDto
            {
                Id = t.Id,
                Name = t.Name,
                MaxDaysPerYear = t.MaxDaysPerYear,
                IsPaid = t.IsPaid,
                IsActive = t.IsActive
            });
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Leave types retrieved.", Data = result });
        }

        [HttpPost("types")]
        [RequirePermission("HR", "ManageLeaveTypes")]
        public async Task<IActionResult> CreateLeaveType([FromBody] CreateLeaveTypeRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            var lt = new LeaveType { Name = dto.Name, MaxDaysPerYear = dto.MaxDaysPerYear, IsPaid = dto.IsPaid };
            var created = await _repo.CreateLeaveTypeAsync(lt);
            return Ok(new CommonApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = "Leave type created.",
                Data = new LeaveTypeResponseDto { Id = created.Id, Name = created.Name, MaxDaysPerYear = created.MaxDaysPerYear, IsPaid = created.IsPaid, IsActive = true }
            });
        }

        // ── Leave Requests ────────────────────────────────────────────────────

        [HttpGet("requests")]
        [RequirePermission("HR", "ViewLeave")]
        public async Task<IActionResult> GetRequests([FromQuery] Guid? employeeId, [FromQuery] string? status)
        {
            var requests = await _repo.GetLeaveRequestsAsync(employeeId, status);
            var result = requests.Select(MapLeaveRequestResponse);
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Leave requests retrieved.", Data = result });
        }

        [HttpGet("requests/{id:guid}")]
        [RequirePermission("HR", "ViewLeave")]
        public async Task<IActionResult> GetRequestById(Guid id)
        {
            var lr = await _repo.GetLeaveRequestByIdAsync(id);
            if (lr == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = "Leave request not found.", Data = null });
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Leave request retrieved.", Data = MapLeaveRequestResponse(lr) });
        }

        [HttpPost("requests")]
        [RequirePermission("HR", "ApplyLeave")]
        public async Task<IActionResult> CreateRequest([FromBody] CreateLeaveRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            if (dto.EndDate < dto.StartDate)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "End date cannot be before start date.", Data = null });

            var lr = new LeaveRequest
            {
                EmployeeId = dto.EmployeeId,
                LeaveTypeId = dto.LeaveTypeId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Reason = dto.Reason
            };
            var created = await _repo.CreateLeaveRequestAsync(lr);
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Leave request submitted.", Data = MapLeaveRequestResponse(created) });
        }

        [HttpPut("requests/approve")]
        [RequirePermission("HR", "ApproveLeave")]
        public async Task<IActionResult> ApproveRequest([FromBody] ApproveLeaveRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            if (dto.Status != "Approved" && dto.Status != "Rejected")
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Status must be 'Approved' or 'Rejected'.", Data = null });

            var lr = await _repo.ApproveLeaveRequestAsync(dto.LeaveRequestId, dto.Status, dto.ApprovedById, dto.ApprovalNote);
            if (lr == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = "Leave request not found.", Data = null });

            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = $"Leave request {dto.Status.ToLower()}.", Data = MapLeaveRequestResponse(lr) });
        }

        // ── Leave Balances ────────────────────────────────────────────────────

        [HttpGet("balances/{employeeId:guid}")]
        [RequirePermission("HR", "ViewLeave")]
        public async Task<IActionResult> GetBalances(Guid employeeId, [FromQuery] int year = 0)
        {
            if (year == 0) year = DateTime.UtcNow.Year;
            var balances = await _repo.GetLeaveBalancesByEmployeeAsync(employeeId, year);
            var result = balances.Select(b => new LeaveBalanceResponseDto
            {
                EmployeeId = b.EmployeeId,
                EmployeeName = b.Employee != null ? $"{b.Employee.FirstName} {b.Employee.LastName}" : string.Empty,
                LeaveTypeName = b.LeaveType?.Name ?? string.Empty,
                Year = b.Year,
                TotalDays = b.TotalDays,
                UsedDays = b.UsedDays,
                RemainingDays = b.RemainingDays
            });
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Leave balances retrieved.", Data = result });
        }

        private static LeaveRequestResponseDto MapLeaveRequestResponse(LeaveRequest lr) => new()
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
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Attendance Controller
    // ═══════════════════════════════════════════════════════════════════════════

    [Route("api/hr/attendance")]
    [ApiController]
    [Authorize]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceRepository _repo;
        public AttendanceController(IAttendanceRepository repo) => _repo = repo;

        [HttpGet]
        [RequirePermission("HR", "ViewAttendance")]
        public async Task<IActionResult> GetAll(
            [FromQuery] Guid? employeeId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var records = await _repo.GetAttendancesAsync(employeeId, from, to);
            return Ok(new CommonApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = "Attendance records retrieved.",
                Data = records.Select(MapAttendanceResponse)
            });
        }

        [HttpPost("mark")]
        [RequirePermission("HR", "MarkAttendance")]
        public async Task<IActionResult> MarkAttendance([FromBody] MarkAttendanceRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            var attendance = new Attendance
            {
                EmployeeId = dto.EmployeeId,
                Date = dto.Date.Date,
                CheckInTime = dto.CheckInTime,
                CheckOutTime = dto.CheckOutTime,
                Status = dto.Status,
                Note = dto.Note
            };

            var result = await _repo.MarkAttendanceAsync(attendance);
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Attendance marked.", Data = MapAttendanceResponse(result) });
        }

        private static AttendanceResponseDto MapAttendanceResponse(Attendance a) => new()
        {
            Id = a.Id,
            EmployeeId = a.EmployeeId,
            EmployeeName = a.Employee != null ? $"{a.Employee.FirstName} {a.Employee.LastName}" : string.Empty,
            Date = a.Date,
            CheckInTime = a.CheckInTime,
            CheckOutTime = a.CheckOutTime,
            WorkedHours = a.WorkedHours,
            Status = a.Status,
            Note = a.Note,
            CreatedAt = a.CreatedAt
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Payroll Controller
    // ═══════════════════════════════════════════════════════════════════════════

    [Route("api/hr/payroll")]
    [ApiController]
    [Authorize]
    public class PayrollController : ControllerBase
    {
        private readonly IPayrollRepository _repo;
        public PayrollController(IPayrollRepository repo) => _repo = repo;

        [HttpGet]
        [RequirePermission("HR", "ViewPayroll")]
        public async Task<IActionResult> GetAll([FromQuery] Guid? employeeId, [FromQuery] int? month, [FromQuery] int? year)
        {
            var payrolls = await _repo.GetPayrollsAsync(employeeId, month, year);
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Payroll records retrieved.", Data = payrolls.Select(MapPayrollResponse) });
        }

        [HttpGet("{id:guid}")]
        [RequirePermission("HR", "ViewPayroll")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var payroll = await _repo.GetPayrollByIdAsync(id);
            if (payroll == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = "Payroll record not found.", Data = null });
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Payroll retrieved.", Data = MapPayrollResponse(payroll) });
        }

        [HttpPost("generate")]
        [RequirePermission("HR", "ProcessPayroll")]
        public async Task<IActionResult> Generate([FromBody] GeneratePayrollRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            try
            {
                var payroll = new Payroll
                {
                    EmployeeId = dto.EmployeeId,
                    Month = dto.Month,
                    Year = dto.Year,
                    Allowances = dto.Allowances,
                    Bonuses = dto.Bonuses,
                    OvertimePay = dto.OvertimePay,
                    Deductions = dto.Deductions,
                    ProcessedById = dto.ProcessedById
                };

                // BasicSalary is auto-fetched in repository from Employee record
                var result = await _repo.GeneratePayrollAsync(payroll);
                return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Payroll generated.", Data = MapPayrollResponse(result) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CommonApiResponse<object> { StatusCode = 500, IsSuccess = false, Message = ex.Message, Data = null });
            }
        }

        [HttpPut("{id:guid}/status")]
        [RequirePermission("HR", "ProcessPayroll")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromQuery] string status)
        {
            var validStatuses = new[] { "Draft", "Approved", "Paid" };
            if (!validStatuses.Contains(status))
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Invalid status.", Data = null });

            var updated = await _repo.UpdatePayrollStatusAsync(id, status);
            if (updated == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = "Payroll not found.", Data = null });

            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Payroll status updated.", Data = MapPayrollResponse(updated) });
        }

        private static PayrollResponseDto MapPayrollResponse(Payroll p) => new()
        {
            Id = p.Id,
            EmployeeId = p.EmployeeId,
            EmployeeName = p.Employee != null ? $"{p.Employee.FirstName} {p.Employee.LastName}" : string.Empty,
            Month = p.Month,
            Year = p.Year,
            BasicSalary = p.BasicSalary,
            Allowances = p.Allowances,
            Bonuses = p.Bonuses,
            OvertimePay = p.OvertimePay,
            Deductions = p.Deductions,
            TaxAmount = p.TaxAmount,
            NetSalary = p.NetSalary,
            WorkingDays = p.WorkingDays,
            PresentDays = p.PresentDays,
            LeaveDays = p.LeaveDays,
            Status = p.Status,
            PaidAt = p.PaidAt,
            CreatedAt = p.CreatedAt
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Performance Controller
    // ═══════════════════════════════════════════════════════════════════════════

    [Route("api/hr/performance")]
    [ApiController]
    [Authorize]
    public class PerformanceController : ControllerBase
    {
        private readonly IPerformanceRepository _repo;
        public PerformanceController(IPerformanceRepository repo) => _repo = repo;

        [HttpGet]
        [RequirePermission("HR", "ViewPerformance")]
        public async Task<IActionResult> GetAll([FromQuery] Guid? employeeId, [FromQuery] string? period)
        {
            var reviews = await _repo.GetReviewsAsync(employeeId, period);
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Reviews retrieved.", Data = reviews.Select(MapResponse) });
        }

        [HttpGet("{id:guid}")]
        [RequirePermission("HR", "ViewPerformance")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var review = await _repo.GetReviewByIdAsync(id);
            if (review == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = "Review not found.", Data = null });
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Review retrieved.", Data = MapResponse(review) });
        }

        [HttpPost]
        [RequirePermission("HR", "CreatePerformanceReview")]
        public async Task<IActionResult> Create([FromBody] CreatePerformanceReviewRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            var review = new PerformanceReview
            {
                EmployeeId = dto.EmployeeId,
                ReviewerId = dto.ReviewerId,
                ReviewDate = dto.ReviewDate,
                Period = dto.Period,
                Rating = dto.Rating,
                Strengths = dto.Strengths,
                Improvements = dto.Improvements,
                Goals = dto.Goals,
                Comments = dto.Comments
            };

            var created = await _repo.CreateReviewAsync(review);
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Performance review created.", Data = MapResponse(created) });
        }

        [HttpPut("{id:guid}/status")]
        [RequirePermission("HR", "CreatePerformanceReview")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromQuery] string status)
        {
            var validStatuses = new[] { "Draft", "Submitted", "Acknowledged" };
            if (!validStatuses.Contains(status))
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Invalid status.", Data = null });

            var updated = await _repo.UpdateStatusAsync(id, status);
            if (updated == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = "Review not found.", Data = null });

            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Review status updated.", Data = MapResponse(updated) });
        }

        private static PerformanceReviewResponseDto MapResponse(PerformanceReview pr) => new()
        {
            Id = pr.Id,
            EmployeeId = pr.EmployeeId,
            EmployeeName = pr.Employee != null ? $"{pr.Employee.FirstName} {pr.Employee.LastName}" : string.Empty,
            ReviewerId = pr.ReviewerId,
            ReviewerName = pr.Reviewer != null ? $"{pr.Reviewer.FirstName} {pr.Reviewer.LastName}" : string.Empty,
            ReviewDate = pr.ReviewDate,
            Period = pr.Period,
            Rating = pr.Rating,
            Strengths = pr.Strengths,
            Improvements = pr.Improvements,
            Goals = pr.Goals,
            Comments = pr.Comments,
            Status = pr.Status,
            CreatedAt = pr.CreatedAt
        };
    }
}
