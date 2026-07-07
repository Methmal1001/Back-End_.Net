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
    // Department Controller
    // ═══════════════════════════════════════════════════════════════════════════

    [Route("api/hr/departments")]
    [ApiController]
    [Authorize]
    public class DepartmentsController : ControllerBase
    {
        private readonly IEmployeeRepository _repo;
        public DepartmentsController(IEmployeeRepository repo) => _repo = repo;

        // GET api/hr/departments/GetDepartments
        [HttpGet("GetDepartments")]
        [RequirePermission("HR", "ViewDepartment")]
        public async Task<IActionResult> GetDepartments()
        {
            var depts = await _repo.GetAllDepartmentsAsync();
            var result = depts.Select(MapDeptResponse);
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Departments retrieved.", Data = result });
        }

        // GET api/hr/departments/GetDepartmentById?id={id}
        [HttpGet("GetDepartmentById")]
        [RequirePermission("HR", "ViewDepartment")]
        public async Task<IActionResult> GetDepartmentById([FromQuery] Guid id)
        {
            var dept = await _repo.GetDepartmentByIdAsync(id);
            if (dept == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = "Department not found.", Data = null });
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Department retrieved.", Data = MapDeptResponse(dept) });
        }

        // POST api/hr/departments/CreateDepartment
        [HttpPost("CreateDepartment")]
        [RequirePermission("HR", "CreateDepartment")]
        public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            var dept = new Department { Name = dto.Name, Description = dto.Description, Code = dto.Code, ManagerId = dto.ManagerId };
            var created = await _repo.CreateDepartmentAsync(dept);
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Department created.", Data = MapDeptResponse(created) });
        }

        // PUT api/hr/departments/UpdateDepartment?id={id}
        [HttpPut("UpdateDepartment")]
        [RequirePermission("HR", "UpdateDepartment")]
        public async Task<IActionResult> UpdateDepartment([FromQuery] Guid id, [FromBody] UpdateDepartmentRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            var updated = await _repo.UpdateDepartmentAsync(id, new Department
            {
                Name = dto.Name,
                Description = dto.Description,
                Code = dto.Code,
                ManagerId = dto.ManagerId,
                IsActive = dto.IsActive
            });

            if (updated == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = "Department not found.", Data = null });

            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Department updated.", Data = MapDeptResponse(updated) });
        }

        private static DepartmentResponseDto MapDeptResponse(Department d) => new()
        {
            Id = d.Id,
            Name = d.Name,
            Description = d.Description,
            Code = d.Code,
            ManagerId = d.ManagerId,
            ManagerName = d.Manager != null ? $"{d.Manager.FirstName} {d.Manager.LastName}" : null,
            IsActive = d.IsActive,
            EmployeeCount = d.Employees?.Count ?? 0,
            CreatedAt = d.CreatedAt
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Job Positions Controller
    // ═══════════════════════════════════════════════════════════════════════════

    [Route("api/hr/job-positions")]
    [ApiController]
    [Authorize]
    public class JobPositionsController : ControllerBase
    {
        private readonly IEmployeeRepository _repo;
        public JobPositionsController(IEmployeeRepository repo) => _repo = repo;

        // GET api/hr/job-positions/GetJobPositions
        [HttpGet("GetJobPositions")]
        [RequirePermission("HR", "ViewJobPosition")]
        public async Task<IActionResult> GetJobPositions([FromQuery] Guid? departmentId)
        {
            var positions = await _repo.GetAllJobPositionsAsync(departmentId);
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Job positions retrieved.", Data = positions.Select(MapResponse) });
        }

        // GET api/hr/job-positions/GetJobPositionById?id={id}
        [HttpGet("GetJobPositionById")]
        [RequirePermission("HR", "ViewJobPosition")]
        public async Task<IActionResult> GetJobPositionById([FromQuery] Guid id)
        {
            var pos = await _repo.GetJobPositionByIdAsync(id);
            if (pos == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = "Job position not found.", Data = null });
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Job position retrieved.", Data = MapResponse(pos) });
        }

        // POST api/hr/job-positions/CreateJobPosition
        [HttpPost("CreateJobPosition")]
        [RequirePermission("HR", "CreateJobPosition")]
        public async Task<IActionResult> CreateJobPosition([FromBody] CreateJobPositionRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            var pos = new JobPosition
            {
                Title = dto.Title,
                Description = dto.Description,
                DepartmentId = dto.DepartmentId,
                Level = dto.Level,
                MinSalary = dto.MinSalary,
                MaxSalary = dto.MaxSalary
            };
            var created = await _repo.CreateJobPositionAsync(pos);
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Job position created.", Data = MapResponse(created) });
        }

        // PUT api/hr/job-positions/UpdateJobPosition?id={id}
        [HttpPut("UpdateJobPosition")]
        [RequirePermission("HR", "UpdateJobPosition")]
        public async Task<IActionResult> UpdateJobPosition([FromQuery] Guid id, [FromBody] UpdateJobPositionRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            var updated = await _repo.UpdateJobPositionAsync(id, new JobPosition
            {
                Title = dto.Title,
                Description = dto.Description,
                DepartmentId = dto.DepartmentId,
                Level = dto.Level,
                MinSalary = dto.MinSalary,
                MaxSalary = dto.MaxSalary,
                IsActive = dto.IsActive
            });

            if (updated == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = "Job position not found.", Data = null });

            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Job position updated.", Data = MapResponse(updated) });
        }

        private static JobPositionResponseDto MapResponse(JobPosition j) => new()
        {
            Id = j.Id,
            Title = j.Title,
            Description = j.Description,
            DepartmentId = j.DepartmentId,
            DepartmentName = j.Department?.Name ?? string.Empty,
            Level = j.Level,
            MinSalary = j.MinSalary,
            MaxSalary = j.MaxSalary,
            IsActive = j.IsActive,
            CreatedAt = j.CreatedAt
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Employees Controller
    // ═══════════════════════════════════════════════════════════════════════════

    [Route("api/hr/employees")]
    [ApiController]
    [Authorize]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeRepository _repo;
        private readonly IDocumentRepository _docRepo;

        public EmployeesController(IEmployeeRepository repo, IDocumentRepository docRepo)
        {
            _repo = repo;
            _docRepo = docRepo;
        }

        // GET api/hr/employees/GetEmployees
        [HttpGet("GetEmployees")]
        [RequirePermission("HR", "ViewEmployee")]
        public async Task<IActionResult> GetEmployees(
            [FromQuery] string? search,
            [FromQuery] Guid? departmentId,
            [FromQuery] string? status,
            [FromQuery] string? sortBy,
            [FromQuery] bool isDescending = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var (items, total) = await _repo.GetAllAsync(search, departmentId, status, sortBy, isDescending, page, pageSize);
            var response = new EmployeeListResponseDto
            {
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Employees = items.Select(MapResponse).ToList()
            };
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Employees retrieved.", Data = response });
        }

        // GET api/hr/employees/GetEmployeeById?id={id}
        [HttpGet("GetEmployeeById")]
        [RequirePermission("HR", "ViewEmployee")]
        public async Task<IActionResult> GetEmployeeById([FromQuery] Guid id)
        {
            var emp = await _repo.GetByIdAsync(id);
            if (emp == null)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = "Employee not found.", Data = null });
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Employee retrieved.", Data = MapResponse(emp) });
        }

        // POST api/hr/employees/CreateEmployee
        [HttpPost("CreateEmployee")]
        [RequirePermission("HR", "CreateEmployee")]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequestDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

                var existing = await _repo.GetByEmailAsync(dto.Email);
                if (existing != null)
                    return Conflict(new CommonApiResponse<object> { StatusCode = 409, IsSuccess = false, Message = "Email already in use.", Data = null });

                var emp = MapFromCreateDto(dto);
                var created = await _repo.CreateAsync(emp);
                return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Employee created.", Data = MapResponse(created) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CommonApiResponse<object> { StatusCode = 500, IsSuccess = false, Message = ex.Message, Data = null });
            }
        }

        // PUT api/hr/employees/UpdateEmployee?id={id}
        [HttpPut("UpdateEmployee")]
        [RequirePermission("HR", "UpdateEmployee")]
        public async Task<IActionResult> UpdateEmployee([FromQuery] Guid id, [FromBody] UpdateEmployeeRequestDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

                var emp = MapFromUpdateDto(dto);
                var updated = await _repo.UpdateAsync(id, emp);
                if (updated == null)
                    return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = "Employee not found.", Data = null });

                return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Employee updated.", Data = MapResponse(updated) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CommonApiResponse<object> { StatusCode = 500, IsSuccess = false, Message = ex.Message, Data = null });
            }
        }

        // DELETE api/hr/employees/DeleteEmployee?id={id}  (soft delete — marks as Terminated)
        [HttpDelete("DeleteEmployee")]
        [RequirePermission("HR", "DeleteEmployee")]
        public async Task<IActionResult> DeleteEmployee([FromQuery] Guid id)
        {
            var deleted = await _repo.DeleteAsync(id);
            if (!deleted)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = "Employee not found.", Data = null });
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Employee terminated.", Data = null });
        }

        // GET api/hr/employees/GetEmployeeDocuments?id={id}
        [HttpGet("GetEmployeeDocuments")]
        [RequirePermission("HR", "ViewEmployee")]
        public async Task<IActionResult> GetEmployeeDocuments([FromQuery] Guid id)
        {
            var docs = await _docRepo.GetDocumentsByEmployeeAsync(id);
            var result = docs.Select(d => new DocumentResponseDto
            {
                Id = d.Id,
                EmployeeId = d.EmployeeId,
                EmployeeName = d.Employee != null ? $"{d.Employee.FirstName} {d.Employee.LastName}" : string.Empty,
                DocumentType = d.DocumentType,
                FileName = d.FileName,
                FileUrl = d.FileUrl,
                ExpiryDate = d.ExpiryDate,
                Note = d.Note,
                UploadedAt = d.UploadedAt
            });
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Documents retrieved.", Data = result });
        }

        // POST api/hr/employees/UploadEmployeeDocument
        [HttpPost("UploadEmployeeDocument")]
        [RequirePermission("HR", "UploadDocument")]
        public async Task<IActionResult> UploadEmployeeDocument([FromBody] UploadDocumentRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new CommonApiResponse<object> { StatusCode = 400, IsSuccess = false, Message = "Validation failed", Data = ModelState });

            var doc = new EmployeeDocument
            {
                EmployeeId = dto.EmployeeId,
                DocumentType = dto.DocumentType,
                FileName = dto.FileName,
                FileUrl = dto.FileUrl,
                ExpiryDate = dto.ExpiryDate,
                Note = dto.Note
            };
            var created = await _docRepo.UploadDocumentAsync(doc);
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Document uploaded.", Data = created.Id });
        }

        // DELETE api/hr/employees/DeleteEmployeeDocument?id={id}
        [HttpDelete("DeleteEmployeeDocument")]
        [RequirePermission("HR", "DeleteDocument")]
        public async Task<IActionResult> DeleteEmployeeDocument([FromQuery] Guid id)
        {
            var deleted = await _docRepo.DeleteDocumentAsync(id);
            if (!deleted)
                return NotFound(new CommonApiResponse<object> { StatusCode = 404, IsSuccess = false, Message = "Document not found.", Data = null });
            return Ok(new CommonApiResponse<object> { StatusCode = 200, IsSuccess = true, Message = "Document deleted.", Data = null });
        }

        // ── Mappers ───────────────────────────────────────────────────────────

        private static Employee MapFromCreateDto(CreateEmployeeRequestDto d) => new()
        {
            FirstName = d.FirstName.Trim(),
            LastName = d.LastName.Trim(),
            Email = d.Email.Trim(),
            Phone = d.Phone,
            NationalId = d.NationalId,
            DateOfBirth = d.DateOfBirth,
            Gender = d.Gender,
            Address = d.Address,
            City = d.City,
            Country = d.Country,
            DepartmentId = d.DepartmentId,
            JobPositionId = d.JobPositionId,
            ManagerId = d.ManagerId,
            JoiningDate = d.JoiningDate,
            EmploymentType = d.EmploymentType,
            BasicSalary = d.BasicSalary,
            BankAccountNo = d.BankAccountNo,
            BankName = d.BankName,
            AppUserId = d.AppUserId
        };

        private static Employee MapFromUpdateDto(UpdateEmployeeRequestDto d)
        {
            var emp = MapFromCreateDto(d);
            emp.Status = d.Status;
            emp.TerminationDate = d.TerminationDate;
            return emp;
        }

        private static EmployeeResponseDto MapResponse(Employee e) => new()
        {
            Id = e.Id,
            EmployeeNo = e.EmployeeNo,
            FirstName = e.FirstName,
            LastName = e.LastName,
            Email = e.Email,
            Phone = e.Phone,
            NationalId = e.NationalId,
            DateOfBirth = e.DateOfBirth,
            Gender = e.Gender,
            Address = e.Address,
            City = e.City,
            Country = e.Country,
            DepartmentId = e.DepartmentId,
            DepartmentName = e.Department?.Name ?? string.Empty,
            JobPositionId = e.JobPositionId,
            JobTitle = e.JobPosition?.Title ?? string.Empty,
            ManagerId = e.ManagerId,
            ManagerName = e.Manager != null ? $"{e.Manager.FirstName} {e.Manager.LastName}" : null,
            JoiningDate = e.JoiningDate,
            TerminationDate = e.TerminationDate,
            EmploymentType = e.EmploymentType,
            Status = e.Status,
            BasicSalary = e.BasicSalary,
            BankAccountNo = e.BankAccountNo,
            BankName = e.BankName,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}
