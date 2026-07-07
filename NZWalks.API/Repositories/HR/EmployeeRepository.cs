using Microsoft.EntityFrameworkCore;
using NZWalks.API.Data;
using NZWalks.API.Models.Domain.HR;

namespace NZWalks.API.Repositories.HR
{
    public interface IEmployeeRepository
    {
        Task<(List<Employee> Items, int Total)> GetAllAsync(
            string? search, Guid? departmentId, string? status,
            string? sortBy, bool isDescending, int page, int pageSize);
        Task<Employee?> GetByIdAsync(Guid id);
        Task<Employee?> GetByEmailAsync(string email);
        Task<Employee> CreateAsync(Employee employee);
        Task<Employee?> UpdateAsync(Guid id, Employee updated);
        Task<bool> DeleteAsync(Guid id);
        Task<List<Department>> GetAllDepartmentsAsync();
        Task<Department?> GetDepartmentByIdAsync(Guid id);
        Task<Department> CreateDepartmentAsync(Department dept);
        Task<Department?> UpdateDepartmentAsync(Guid id, Department updated);
        Task<List<JobPosition>> GetAllJobPositionsAsync(Guid? departmentId);
        Task<JobPosition?> GetJobPositionByIdAsync(Guid id);
        Task<JobPosition> CreateJobPositionAsync(JobPosition pos);
        Task<JobPosition?> UpdateJobPositionAsync(Guid id, JobPosition updated);
        Task<string> GenerateEmployeeNoAsync();
    }

    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly HrDbContext _db;
        public EmployeeRepository(HrDbContext db) => _db = db;

        public async Task<(List<Employee> Items, int Total)> GetAllAsync(
            string? search, Guid? departmentId, string? status,
            string? sortBy, bool isDescending, int page, int pageSize)
        {
            var query = _db.Employees
                .Include(e => e.Department).Include(e => e.JobPosition).Include(e => e.Manager)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(e =>
                    e.FirstName.Contains(search) || e.LastName.Contains(search) ||
                    e.Email.Contains(search) || e.EmployeeNo.Contains(search));

            if (departmentId.HasValue)
                query = query.Where(e => e.DepartmentId == departmentId.Value);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(e => e.Status == status);

            query = sortBy?.ToLower() switch
            {
                "name" => isDescending ? query.OrderByDescending(e => e.FirstName) : query.OrderBy(e => e.FirstName),
                "joiningdate" => isDescending ? query.OrderByDescending(e => e.JoiningDate) : query.OrderBy(e => e.JoiningDate),
                "employeeno" => isDescending ? query.OrderByDescending(e => e.EmployeeNo) : query.OrderBy(e => e.EmployeeNo),
                _ => query.OrderBy(e => e.FirstName)
            };

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, total);
        }

        public async Task<Employee?> GetByIdAsync(Guid id)
            => await _db.Employees
                .Include(e => e.Department).Include(e => e.JobPosition).Include(e => e.Manager)
                .FirstOrDefaultAsync(e => e.Id == id);

        public async Task<Employee?> GetByEmailAsync(string email)
            => await _db.Employees.FirstOrDefaultAsync(e => e.Email == email.ToLower());

        public async Task<Employee> CreateAsync(Employee employee)
        {
            employee.EmployeeNo = await GenerateEmployeeNoAsync();
            employee.Email = employee.Email.ToLower().Trim();
            _db.Employees.Add(employee);
            await _db.SaveChangesAsync();
            return employee;
        }

        public async Task<Employee?> UpdateAsync(Guid id, Employee updated)
        {
            var existing = await _db.Employees.FindAsync(id);
            if (existing == null) return null;

            existing.FirstName = updated.FirstName;
            existing.LastName = updated.LastName;
            existing.Email = updated.Email.ToLower().Trim();
            existing.Phone = updated.Phone;
            existing.NationalId = updated.NationalId;
            existing.DateOfBirth = updated.DateOfBirth;
            existing.Gender = updated.Gender;
            existing.Address = updated.Address;
            existing.City = updated.City;
            existing.Country = updated.Country;
            existing.DepartmentId = updated.DepartmentId;
            existing.JobPositionId = updated.JobPositionId;
            existing.ManagerId = updated.ManagerId;
            existing.JoiningDate = updated.JoiningDate;
            existing.TerminationDate = updated.TerminationDate;
            existing.EmploymentType = updated.EmploymentType;
            existing.Status = updated.Status;
            existing.BasicSalary = updated.BasicSalary;
            existing.BankAccountNo = updated.BankAccountNo;
            existing.BankName = updated.BankName;
            if (updated.AppUserId.HasValue)
                existing.AppUserId = updated.AppUserId;
            existing.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var emp = await _db.Employees.FindAsync(id);
            if (emp == null) return false;
            emp.Status = "Terminated";
            emp.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<string> GenerateEmployeeNoAsync()
        {
            var count = await _db.Employees.CountAsync();
            return $"EMP-{(count + 1):D4}";
        }

        public async Task<List<Department>> GetAllDepartmentsAsync()
            => await _db.Departments.Include(d => d.Manager).Include(d => d.Employees)
                .Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();

        public async Task<Department?> GetDepartmentByIdAsync(Guid id)
            => await _db.Departments.Include(d => d.Manager).Include(d => d.Employees)
                .FirstOrDefaultAsync(d => d.Id == id);

        public async Task<Department> CreateDepartmentAsync(Department dept)
        {
            _db.Departments.Add(dept);
            await _db.SaveChangesAsync();
            return dept;
        }

        public async Task<Department?> UpdateDepartmentAsync(Guid id, Department updated)
        {
            var existing = await _db.Departments.FindAsync(id);
            if (existing == null) return null;
            existing.Name = updated.Name;
            existing.Description = updated.Description;
            existing.Code = updated.Code;
            existing.ManagerId = updated.ManagerId;
            existing.IsActive = updated.IsActive;
            await _db.SaveChangesAsync();
            return existing;
        }

        public async Task<List<JobPosition>> GetAllJobPositionsAsync(Guid? departmentId)
        {
            var query = _db.JobPositions.Include(j => j.Department).AsQueryable();
            if (departmentId.HasValue) query = query.Where(j => j.DepartmentId == departmentId.Value);
            return await query.Where(j => j.IsActive).OrderBy(j => j.Title).ToListAsync();
        }

        public async Task<JobPosition?> GetJobPositionByIdAsync(Guid id)
            => await _db.JobPositions.Include(j => j.Department).FirstOrDefaultAsync(j => j.Id == id);

        public async Task<JobPosition> CreateJobPositionAsync(JobPosition pos)
        {
            _db.JobPositions.Add(pos);
            await _db.SaveChangesAsync();
            return pos;
        }

        public async Task<JobPosition?> UpdateJobPositionAsync(Guid id, JobPosition updated)
        {
            var existing = await _db.JobPositions.FindAsync(id);
            if (existing == null) return null;
            existing.Title = updated.Title;
            existing.Description = updated.Description;
            existing.DepartmentId = updated.DepartmentId;
            existing.Level = updated.Level;
            existing.MinSalary = updated.MinSalary;
            existing.MaxSalary = updated.MaxSalary;
            existing.IsActive = updated.IsActive;
            await _db.SaveChangesAsync();
            return existing;
        }
    }
}