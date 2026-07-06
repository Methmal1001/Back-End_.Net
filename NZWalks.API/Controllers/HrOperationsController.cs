using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NZWalks.API.Controllers;
using NZWalks.API.Helpers;
using NZWalks.API.Models.Domain.HR;
using NZWalks.API.Models.DTO.HR;
using NZWalks.API.Repositories.HR;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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
        private readonly IAuthRepository _authRepo;

        public LeaveController(ILeaveRepository repo, IAuthRepository authRepo)
        {
            _repo = repo;
            _authRepo = authRepo;
        }

        // ── Leave Types ───────────────────────────────────────────────────────

        [HttpGet("GetLeaveTypes")]
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

        [HttpPost("CreateLeaveType")]
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

        [HttpGet("GetLeaveRequests")]
        [RequirePermission("HR", "ViewLeave")]
        public async Task<IActionResult> GetLeaveRequests([FromQuery] Guid? employeeId, [FromQuery] string? status)
        {
            if (!HrTierRoles.IsHrManagement(User))
            {
                var callerEmployeeId = await GetCallerEmployeeIdAsync();
                if (callerEmployeeId == null)
                    return StatusCode(403, new CommonApiResponse<object> { StatusCode = 403, IsSuccess = false, Message = "No linked employee record for this account.", Data = null });
                employeeId = callerEmployeeId;
            }

            var requests = await _repo.GetLeaveRequestsAsync(employeeId, status);
            var result = requests.Select(MapLeaveRequestResponse);
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Leave requests retrieved.", Data = result });
        }

        [HttpGet("GetLeaveRequestById")]
        [RequirePermission("HR", "ViewLeave")]
        public async Task<IActionResult> GetLeaveRequestById([FromQuery] Guid id)
        {
            var lr = await _repo.GetLeaveRequestByIdAsync(id);
            if (lr == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = "Leave request not found.", Data = null });

            if (!HrTierRoles.IsHrManagement(User))
            {
                var callerEmployeeId = await GetCallerEmployeeIdAsync();
                if (callerEmployeeId == null || lr.EmployeeId != callerEmployeeId.Value)
                    return StatusCode(403, new CommonApiResponse<object> { StatusCode = 403, IsSuccess = false, Message = "You may only view your own leave requests.", Data = null });
            }

            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Leave request retrieved.", Data = MapLeaveRequestResponse(lr) });
        }

        [HttpPost("CreateLeaveRequest")]
        [RequirePermission("HR", "ApplyLeave")]
        public async Task<IActionResult> CreateLeaveRequest([FromBody] CreateLeaveRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            if (dto.EndDate < dto.StartDate)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "End date cannot be before start date.", Data = null });

            if (!HrTierRoles.IsHrManagement(User))
            {
                var callerEmployeeId = await GetCallerEmployeeIdAsync();
                if (callerEmployeeId == null)
                    return StatusCode(403, new CommonApiResponse<object> { StatusCode = 403, IsSuccess = false, Message = "No linked employee record for this account.", Data = null });
                dto.EmployeeId = callerEmployeeId.Value;
            }

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

        // ── GET api/hr/leave/GetPendingApprovals ────────────────────────────────
        [HttpGet("GetPendingApprovals")]
        [RequirePermission("HR", "ApproveLeave")]
        public async Task<IActionResult> GetPendingApprovals()
        {
            List<LeaveRequest> records;

            if (HrTierRoles.IsHrManagement(User))
            {
                records = await _repo.GetAllPendingApprovalsAsync();
            }
            else
            {
                var callerEmployeeId = await GetCallerEmployeeIdAsync();
                if (callerEmployeeId == null)
                    return StatusCode(403, new CommonApiResponse<object> { StatusCode = 403, IsSuccess = false, Message = "No linked employee record for this account.", Data = null });

                records = await _repo.GetPendingApprovalsForManagerAsync(callerEmployeeId.Value);
            }

            return Ok(new CommonApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = "Pending approvals retrieved.",
                Data = records.Select(MapLeaveRequestResponse)
            });
        }

        [HttpPut("ApproveLeaveRequest")]
        [RequirePermission("HR", "ApproveLeave")]
        public async Task<IActionResult> ApproveLeaveRequest([FromBody] ApproveLeaveRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            if (dto.Status != "Approved" && dto.Status != "Rejected")
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Status must be 'Approved' or 'Rejected'.", Data = null });

            var callerEmployeeId = await GetCallerEmployeeIdAsync();
            if (callerEmployeeId == null)
                return StatusCode(403, new CommonApiResponse<object> { StatusCode = 403, IsSuccess = false, Message = "No linked employee record for this account.", Data = null });

            var existing = await _repo.GetLeaveRequestByIdAsync(dto.LeaveRequestId);
            if (existing == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = "Leave request not found.", Data = null });

            if (!HrTierRoles.IsHrManagement(User) && existing.Employee.ManagerId != callerEmployeeId.Value)
                return StatusCode(403, new CommonApiResponse<object> { StatusCode = 403, IsSuccess = false, Message = "You are not this employee's manager.", Data = null });

            var lr = await _repo.ApproveLeaveRequestAsync(dto.LeaveRequestId, dto.Status, callerEmployeeId.Value, dto.ApprovalNote);
            if (lr == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = "Leave request not found.", Data = null });

            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = $"Leave request {dto.Status.ToLower()}.", Data = MapLeaveRequestResponse(lr) });
        }

        // ── Leave Balances ────────────────────────────────────────────────────

        [HttpGet("GetLeaveBalances")]
        [RequirePermission("HR", "ViewLeave")]
        public async Task<IActionResult> GetLeaveBalances([FromQuery] Guid employeeId, [FromQuery] int year = 0)
        {
            if (!HrTierRoles.IsHrManagement(User))
            {
                var callerEmployeeId = await GetCallerEmployeeIdAsync();
                if (callerEmployeeId == null)
                    return StatusCode(403, new CommonApiResponse<object> { StatusCode = 403, IsSuccess = false, Message = "No linked employee record for this account.", Data = null });
                employeeId = callerEmployeeId.Value;
            }

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

        [HttpPost("SetLeaveBalance")]
        [RequirePermission("HR", "ManageLeaveBalances")]
        public async Task<IActionResult> SetLeaveBalance([FromBody] SetLeaveBalanceRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            var totalDays = (int)Math.Round(dto.TotalDays, MidpointRounding.AwayFromZero);
            var balance = await _repo.UpsertLeaveBalanceAsync(dto.EmployeeId, dto.LeaveTypeId, dto.Year, totalDays);

            return Ok(new CommonApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = "Leave balance set.",
                Data = new
                {
                    EmployeeId = balance.EmployeeId,
                    LeaveTypeId = balance.LeaveTypeId,
                    Year = balance.Year,
                    TotalDays = balance.TotalDays,
                    UsedDays = balance.UsedDays,
                    RemainingDays = balance.RemainingDays
                }
            });
        }

        private async Task<Guid?> GetCallerEmployeeIdAsync()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId)) return null;

            var employee = await _authRepo.GetEmployeeLinkAsync(userId);
            return employee?.Id;
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
        private readonly IAuthRepository _authRepo;

        public AttendanceController(IAttendanceRepository repo, IAuthRepository authRepo)
        {
            _repo = repo;
            _authRepo = authRepo;
        }

        [HttpGet("GetAttendanceRecords")]
        [RequirePermission("HR", "ViewAttendance")]
        public async Task<IActionResult> GetAttendanceRecords(
            [FromQuery] Guid? employeeId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            if (!HrTierRoles.IsHrManagement(User))
            {
                var callerEmployeeId = await GetCallerEmployeeIdAsync();
                if (callerEmployeeId == null)
                    return StatusCode(403, new CommonApiResponse<object> { StatusCode = 403, IsSuccess = false, Message = "No linked employee record for this account.", Data = null });
                employeeId = callerEmployeeId;
            }

            var records = await _repo.GetAttendancesAsync(employeeId, from, to);
            return Ok(new CommonApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = "Attendance records retrieved.",
                Data = records.Select(MapAttendanceResponse)
            });
        }

        [HttpPost("MarkAttendance")]
        [RequirePermission("HR", "MarkAttendance")]
        public async Task<IActionResult> MarkAttendance([FromBody] MarkAttendanceRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            if (!HrTierRoles.IsHrManagement(User))
            {
                var callerEmployeeId = await GetCallerEmployeeIdAsync();
                if (callerEmployeeId == null)
                    return StatusCode(403, new CommonApiResponse<object> { StatusCode = 403, IsSuccess = false, Message = "No linked employee record for this account.", Data = null });
                dto.EmployeeId = callerEmployeeId.Value;
            }

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

        // ── GET api/hr/attendance/GetPendingApprovals ───────────────────────────
        [HttpGet("GetPendingApprovals")]
        [RequirePermission("HR", "ApproveAttendance")]
        public async Task<IActionResult> GetPendingApprovals()
        {
            List<Attendance> records;

            if (HrTierRoles.IsHrManagement(User))
            {
                records = await _repo.GetAllPendingApprovalsAsync();
            }
            else
            {
                var callerEmployeeId = await GetCallerEmployeeIdAsync();
                if (callerEmployeeId == null)
                    return StatusCode(403, new CommonApiResponse<object> { StatusCode = 403, IsSuccess = false, Message = "No linked employee record for this account.", Data = null });

                records = await _repo.GetPendingApprovalsForManagerAsync(callerEmployeeId.Value);
            }

            return Ok(new CommonApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = "Pending approvals retrieved.",
                Data = records.Select(MapAttendanceResponse)
            });
        }

        // ── PUT api/hr/attendance/ApproveAttendance?id={id} ─────────────────────
        [HttpPut("ApproveAttendance")]
        [RequirePermission("HR", "ApproveAttendance")]
        public async Task<IActionResult> ApproveAttendance([FromQuery] Guid id)
        {
            var callerEmployeeId = await GetCallerEmployeeIdAsync();
            if (callerEmployeeId == null)
                return StatusCode(403, new CommonApiResponse<object> { StatusCode = 403, IsSuccess = false, Message = "No linked employee record for this account.", Data = null });

            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = "Attendance record not found.", Data = null });

            if (!HrTierRoles.IsHrManagement(User) && existing.Employee.ManagerId != callerEmployeeId.Value)
                return StatusCode(403, new CommonApiResponse<object> { StatusCode = 403, IsSuccess = false, Message = "You are not this employee's manager.", Data = null });

            var result = await _repo.ApproveAsync(id, callerEmployeeId.Value);
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Attendance approved.", Data = MapAttendanceResponse(result!) });
        }

        // ── PUT api/hr/attendance/RejectAttendance?id={id} ──────────────────────
        [HttpPut("RejectAttendance")]
        [RequirePermission("HR", "ApproveAttendance")]
        public async Task<IActionResult> RejectAttendance([FromQuery] Guid id, [FromBody] RejectAttendanceRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            var callerEmployeeId = await GetCallerEmployeeIdAsync();
            if (callerEmployeeId == null)
                return StatusCode(403, new CommonApiResponse<object> { StatusCode = 403, IsSuccess = false, Message = "No linked employee record for this account.", Data = null });

            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = "Attendance record not found.", Data = null });

            if (!HrTierRoles.IsHrManagement(User) && existing.Employee.ManagerId != callerEmployeeId.Value)
                return StatusCode(403, new CommonApiResponse<object> { StatusCode = 403, IsSuccess = false, Message = "You are not this employee's manager.", Data = null });

            var result = await _repo.RejectAsync(id, callerEmployeeId.Value, dto.Reason);
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Attendance rejected.", Data = MapAttendanceResponse(result!) });
        }

        private async Task<Guid?> GetCallerEmployeeIdAsync()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId)) return null;

            var employee = await _authRepo.GetEmployeeLinkAsync(userId);
            return employee?.Id;
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
            ApprovalStatus = a.ApprovalStatus,
            ApprovedByName = a.ApprovedByEmployee != null ? $"{a.ApprovedByEmployee.FirstName} {a.ApprovedByEmployee.LastName}" : null,
            ApprovedAt = a.ApprovedAt,
            RejectionReason = a.RejectionReason,
            CreatedAt = a.CreatedAt
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Overtime Controller
    // ═══════════════════════════════════════════════════════════════════════════

    [Route("api/hr/overtime")]
    [ApiController]
    [Authorize]
    public class OvertimeController : ControllerBase
    {
        private readonly IOvertimeRepository _repo;
        private readonly IAuthRepository _authRepo;

        public OvertimeController(IOvertimeRepository repo, IAuthRepository authRepo)
        {
            _repo = repo;
            _authRepo = authRepo;
        }

        // ── GET api/hr/overtime/GetOvertimeRequests ─────────────────────────────
        [HttpGet("GetOvertimeRequests")]
        [RequirePermission("HR", "ViewOvertime")]
        public async Task<IActionResult> GetOvertimeRequests([FromQuery] Guid? employeeId, [FromQuery] string? status)
        {
            if (!HrTierRoles.IsHrManagement(User))
            {
                var callerEmployeeId = await GetCallerEmployeeIdAsync();
                if (callerEmployeeId == null)
                    return StatusCode(403, new CommonApiResponse<object> { StatusCode = 403, IsSuccess = false, Message = "No linked employee record for this account.", Data = null });
                employeeId = callerEmployeeId;
            }

            var records = await _repo.GetOvertimeRequestsAsync(employeeId, status);
            return Ok(new CommonApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = "Overtime requests retrieved.",
                Data = records.Select(MapOvertimeResponse)
            });
        }

        // ── POST api/hr/overtime/CreateOvertimeRequest ──────────────────────────
        [HttpPost("CreateOvertimeRequest")]
        [RequirePermission("HR", "ApplyOvertime")]
        public async Task<IActionResult> CreateOvertimeRequest([FromBody] CreateOvertimeRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            if (!HrTierRoles.IsHrManagement(User))
            {
                var callerEmployeeId = await GetCallerEmployeeIdAsync();
                if (callerEmployeeId == null)
                    return StatusCode(403, new CommonApiResponse<object> { StatusCode = 403, IsSuccess = false, Message = "No linked employee record for this account.", Data = null });
                dto.EmployeeId = callerEmployeeId.Value;
            }

            var request = new OvertimeRequest
            {
                EmployeeId = dto.EmployeeId,
                Date = dto.Date.Date,
                Hours = dto.Hours,
                Reason = dto.Reason
            };
            var created = await _repo.CreateAsync(request);
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Overtime request submitted.", Data = MapOvertimeResponse(created) });
        }

        // ── GET api/hr/overtime/GetPendingApprovals ──────────────────────────────
        [HttpGet("GetPendingApprovals")]
        [RequirePermission("HR", "ApproveOvertime")]
        public async Task<IActionResult> GetPendingApprovals()
        {
            List<OvertimeRequest> records;

            if (HrTierRoles.IsHrManagement(User))
            {
                records = await _repo.GetAllPendingApprovalsAsync();
            }
            else
            {
                var callerEmployeeId = await GetCallerEmployeeIdAsync();
                if (callerEmployeeId == null)
                    return StatusCode(403, new CommonApiResponse<object> { StatusCode = 403, IsSuccess = false, Message = "No linked employee record for this account.", Data = null });

                records = await _repo.GetPendingApprovalsForManagerAsync(callerEmployeeId.Value);
            }

            return Ok(new CommonApiResponse<object>
            {
                StatusCode = 200,
                IsSuccess = true,
                Message = "Pending approvals retrieved.",
                Data = records.Select(MapOvertimeResponse)
            });
        }

        // ── PUT api/hr/overtime/ApproveOvertime?id={id} ─────────────────────────
        [HttpPut("ApproveOvertime")]
        [RequirePermission("HR", "ApproveOvertime")]
        public async Task<IActionResult> ApproveOvertime([FromQuery] Guid id)
        {
            var callerEmployeeId = await GetCallerEmployeeIdAsync();
            if (callerEmployeeId == null)
                return StatusCode(403, new CommonApiResponse<object> { StatusCode = 403, IsSuccess = false, Message = "No linked employee record for this account.", Data = null });

            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = "Overtime request not found.", Data = null });

            if (!HrTierRoles.IsHrManagement(User) && existing.Employee.ManagerId != callerEmployeeId.Value)
                return StatusCode(403, new CommonApiResponse<object> { StatusCode = 403, IsSuccess = false, Message = "You are not this employee's manager.", Data = null });

            var result = await _repo.ApproveAsync(id, callerEmployeeId.Value);
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Overtime approved.", Data = MapOvertimeResponse(result!) });
        }

        // ── PUT api/hr/overtime/RejectOvertime?id={id} ──────────────────────────
        [HttpPut("RejectOvertime")]
        [RequirePermission("HR", "ApproveOvertime")]
        public async Task<IActionResult> RejectOvertime([FromQuery] Guid id, [FromBody] RejectOvertimeRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            var callerEmployeeId = await GetCallerEmployeeIdAsync();
            if (callerEmployeeId == null)
                return StatusCode(403, new CommonApiResponse<object> { StatusCode = 403, IsSuccess = false, Message = "No linked employee record for this account.", Data = null });

            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = "Overtime request not found.", Data = null });

            if (!HrTierRoles.IsHrManagement(User) && existing.Employee.ManagerId != callerEmployeeId.Value)
                return StatusCode(403, new CommonApiResponse<object> { StatusCode = 403, IsSuccess = false, Message = "You are not this employee's manager.", Data = null });

            var result = await _repo.RejectAsync(id, callerEmployeeId.Value, dto.Reason);
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Overtime rejected.", Data = MapOvertimeResponse(result!) });
        }

        private async Task<Guid?> GetCallerEmployeeIdAsync()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId)) return null;

            var employee = await _authRepo.GetEmployeeLinkAsync(userId);
            return employee?.Id;
        }

        private static OvertimeResponseDto MapOvertimeResponse(OvertimeRequest o) => new()
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

        [HttpGet("GetPayrolls")]
        [RequirePermission("HR", "ViewPayroll")]
        public async Task<IActionResult> GetPayrolls([FromQuery] Guid? employeeId, [FromQuery] int? month, [FromQuery] int? year)
        {
            var payrolls = await _repo.GetPayrollsAsync(employeeId, month, year);
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Payroll records retrieved.", Data = payrolls.Select(MapPayrollResponse) });
        }

        [HttpGet("GetPayrollById")]
        [RequirePermission("HR", "ViewPayroll")]
        public async Task<IActionResult> GetPayrollById([FromQuery] Guid id)
        {
            var payroll = await _repo.GetPayrollByIdAsync(id);
            if (payroll == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = "Payroll record not found.", Data = null });
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Payroll retrieved.", Data = MapPayrollResponse(payroll) });
        }

        [HttpPost("GeneratePayroll")]
        [RequirePermission("HR", "ProcessPayroll")]
        public async Task<IActionResult> GeneratePayroll([FromBody] GeneratePayrollRequestDto dto)
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

        [HttpPut("UpdatePayrollStatus")]
        [RequirePermission("HR", "ProcessPayroll")]
        public async Task<IActionResult> UpdatePayrollStatus([FromQuery] Guid id, [FromQuery] string status)
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

        [HttpGet("GetPerformanceReviews")]
        [RequirePermission("HR", "ViewPerformance")]
        public async Task<IActionResult> GetPerformanceReviews([FromQuery] Guid? employeeId, [FromQuery] string? period)
        {
            var reviews = await _repo.GetReviewsAsync(employeeId, period);
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Reviews retrieved.", Data = reviews.Select(MapResponse) });
        }

        [HttpGet("GetPerformanceReviewById")]
        [RequirePermission("HR", "ViewPerformance")]
        public async Task<IActionResult> GetPerformanceReviewById([FromQuery] Guid id)
        {
            var review = await _repo.GetReviewByIdAsync(id);
            if (review == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = "Review not found.", Data = null });
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Review retrieved.", Data = MapResponse(review) });
        }

        [HttpPost("CreatePerformanceReview")]
        [RequirePermission("HR", "CreatePerformanceReview")]
        public async Task<IActionResult> CreatePerformanceReview([FromBody] CreatePerformanceReviewRequestDto dto)
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

        [HttpPut("UpdatePerformanceReviewStatus")]
        [RequirePermission("HR", "CreatePerformanceReview")]
        public async Task<IActionResult> UpdatePerformanceReviewStatus([FromQuery] Guid id, [FromQuery] string status)
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
