using Microsoft.EntityFrameworkCore;
using NZWalks.API.Data;
using NZWalks.API.Models.Domain.HR;

namespace NZWalks.API.Repositories.HR
{
    // ── Leave ─────────────────────────────────────────────────────────────────

    public interface ILeaveRepository
    {
        Task<List<LeaveType>> GetAllLeaveTypesAsync();
        Task<LeaveType> CreateLeaveTypeAsync(LeaveType lt);
        Task<LeaveRequest> CreateLeaveRequestAsync(LeaveRequest lr);
        Task<LeaveRequest?> GetLeaveRequestByIdAsync(Guid id);
        Task<List<LeaveRequest>> GetLeaveRequestsAsync(Guid? employeeId, string? status);
        Task<LeaveRequest?> ApproveLeaveRequestAsync(Guid id, string status, Guid approvedById, string? note);
        Task<LeaveBalance?> GetLeaveBalanceAsync(Guid employeeId, Guid leaveTypeId, int year);
        Task<List<LeaveBalance>> GetLeaveBalancesByEmployeeAsync(Guid employeeId, int year);
        Task<LeaveBalance> UpsertLeaveBalanceAsync(Guid employeeId, Guid leaveTypeId, int year, int totalDays);
    }

    public class LeaveRepository : ILeaveRepository
    {
        private readonly HrDbContext _db;
        public LeaveRepository(HrDbContext db) => _db = db;

        public async Task<List<LeaveType>> GetAllLeaveTypesAsync()
            => await _db.LeaveTypes.Where(lt => lt.IsActive).ToListAsync();

        public async Task<LeaveType> CreateLeaveTypeAsync(LeaveType lt)
        {
            _db.LeaveTypes.Add(lt);
            await _db.SaveChangesAsync();
            return lt;
        }

        public async Task<LeaveRequest> CreateLeaveRequestAsync(LeaveRequest lr)
        {
            lr.TotalDays = (lr.EndDate - lr.StartDate).Days + 1;
            _db.LeaveRequests.Add(lr);
            await _db.SaveChangesAsync();
            return lr;
        }

        public async Task<LeaveRequest?> GetLeaveRequestByIdAsync(Guid id)
            => await _db.LeaveRequests
                .Include(lr => lr.Employee).Include(lr => lr.LeaveType)
                .FirstOrDefaultAsync(lr => lr.Id == id);

        public async Task<List<LeaveRequest>> GetLeaveRequestsAsync(Guid? employeeId, string? status)
        {
            var query = _db.LeaveRequests
                .Include(lr => lr.Employee).Include(lr => lr.LeaveType).AsQueryable();
            if (employeeId.HasValue) query = query.Where(lr => lr.EmployeeId == employeeId.Value);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(lr => lr.Status == status);
            return await query.OrderByDescending(lr => lr.CreatedAt).ToListAsync();
        }

        public async Task<LeaveRequest?> ApproveLeaveRequestAsync(
            Guid id, string status, Guid approvedById, string? note)
        {
            var lr = await _db.LeaveRequests.FindAsync(id);
            if (lr == null) return null;
            lr.Status = status;
            lr.ApprovedById = approvedById;
            lr.ApprovalNote = note;
            lr.ApprovedAt = DateTime.UtcNow;

            if (status == "Approved")
            {
                var balance = await GetLeaveBalanceAsync(lr.EmployeeId, lr.LeaveTypeId, lr.StartDate.Year);
                if (balance != null)
                {
                    balance.UsedDays += lr.TotalDays;
                    _db.LeaveBalances.Update(balance);
                }
            }
            await _db.SaveChangesAsync();
            return lr;
        }

        public async Task<LeaveBalance?> GetLeaveBalanceAsync(Guid employeeId, Guid leaveTypeId, int year)
            => await _db.LeaveBalances.FirstOrDefaultAsync(
                lb => lb.EmployeeId == employeeId && lb.LeaveTypeId == leaveTypeId && lb.Year == year);

        public async Task<List<LeaveBalance>> GetLeaveBalancesByEmployeeAsync(Guid employeeId, int year)
            => await _db.LeaveBalances
                .Include(lb => lb.LeaveType).Include(lb => lb.Employee)
                .Where(lb => lb.EmployeeId == employeeId && lb.Year == year)
                .ToListAsync();

        public async Task<LeaveBalance> UpsertLeaveBalanceAsync(Guid employeeId, Guid leaveTypeId, int year, int totalDays)
        {
            var existing = await GetLeaveBalanceAsync(employeeId, leaveTypeId, year);
            if (existing == null)
            {
                existing = new LeaveBalance
                {
                    EmployeeId = employeeId,
                    LeaveTypeId = leaveTypeId,
                    Year = year,
                    TotalDays = totalDays,
                    UsedDays = 0
                };
                _db.LeaveBalances.Add(existing);
            }
            else
            {
                existing.TotalDays = totalDays;
                _db.LeaveBalances.Update(existing);
            }
            await _db.SaveChangesAsync();
            return existing;
        }
    }

    // ── Attendance ────────────────────────────────────────────────────────────

    public interface IAttendanceRepository
    {
        Task<Attendance> MarkAttendanceAsync(Attendance attendance);
        Task<List<Attendance>> GetAttendancesAsync(Guid? employeeId, DateTime? from, DateTime? to);
        Task<Attendance?> GetByEmployeeAndDateAsync(Guid employeeId, DateTime date);
        Task<Attendance?> GetByIdAsync(Guid id);
        Task<List<Attendance>> GetPendingApprovalsForManagerAsync(Guid managerEmployeeId);
        Task<Attendance?> ApproveAsync(Guid id, Guid approverEmployeeId);
        Task<Attendance?> RejectAsync(Guid id, Guid approverEmployeeId, string reason);
    }

    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly HrDbContext _db;
        public AttendanceRepository(HrDbContext db) => _db = db;

        public async Task<Attendance> MarkAttendanceAsync(Attendance attendance)
        {
            var existing = await GetByEmployeeAndDateAsync(attendance.EmployeeId, attendance.Date.Date);
            if (existing != null)
            {
                existing.CheckInTime = attendance.CheckInTime;
                existing.CheckOutTime = attendance.CheckOutTime;
                existing.Status = attendance.Status;
                existing.Note = attendance.Note;
                if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
                    existing.WorkedHours = (attendance.CheckOutTime - attendance.CheckInTime)!.Value.TotalHours;
                _db.Attendances.Update(existing);
                await _db.SaveChangesAsync();
                return existing;
            }

            if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
                attendance.WorkedHours = (attendance.CheckOutTime - attendance.CheckInTime)!.Value.TotalHours;

            var employee = await _db.Employees.FindAsync(attendance.EmployeeId);
            attendance.ApprovalStatus = employee?.ManagerId == null ? "Approved" : "Pending";

            _db.Attendances.Add(attendance);
            await _db.SaveChangesAsync();
            return attendance;
        }

        public async Task<List<Attendance>> GetAttendancesAsync(Guid? employeeId, DateTime? from, DateTime? to)
        {
            var query = _db.Attendances.Include(a => a.Employee).Include(a => a.ApprovedByEmployee).AsQueryable();
            if (employeeId.HasValue) query = query.Where(a => a.EmployeeId == employeeId.Value);
            if (from.HasValue) query = query.Where(a => a.Date >= from.Value.Date);
            if (to.HasValue) query = query.Where(a => a.Date <= to.Value.Date);
            return await query.OrderByDescending(a => a.Date).ToListAsync();
        }

        public async Task<Attendance?> GetByEmployeeAndDateAsync(Guid employeeId, DateTime date)
            => await _db.Attendances.FirstOrDefaultAsync(
                a => a.EmployeeId == employeeId && a.Date.Date == date.Date);

        public async Task<Attendance?> GetByIdAsync(Guid id)
            => await _db.Attendances.Include(a => a.Employee).Include(a => a.ApprovedByEmployee)
                .FirstOrDefaultAsync(a => a.Id == id);

        public async Task<List<Attendance>> GetPendingApprovalsForManagerAsync(Guid managerEmployeeId)
            => await _db.Attendances
                .Include(a => a.Employee).Include(a => a.ApprovedByEmployee)
                .Where(a => a.Employee.ManagerId == managerEmployeeId && a.ApprovalStatus == "Pending")
                .OrderByDescending(a => a.Date)
                .ToListAsync();

        public async Task<Attendance?> ApproveAsync(Guid id, Guid approverEmployeeId)
        {
            var att = await _db.Attendances.Include(a => a.Employee).Include(a => a.ApprovedByEmployee)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (att == null) return null;

            att.ApprovalStatus = "Approved";
            att.ApprovedByEmployeeId = approverEmployeeId;
            att.ApprovedAt = DateTime.UtcNow;
            att.RejectionReason = null;
            await _db.SaveChangesAsync();
            return att;
        }

        public async Task<Attendance?> RejectAsync(Guid id, Guid approverEmployeeId, string reason)
        {
            var att = await _db.Attendances.Include(a => a.Employee).Include(a => a.ApprovedByEmployee)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (att == null) return null;

            att.ApprovalStatus = "Rejected";
            att.ApprovedByEmployeeId = approverEmployeeId;
            att.ApprovedAt = DateTime.UtcNow;
            att.RejectionReason = reason;
            await _db.SaveChangesAsync();
            return att;
        }
    }

    // ── Payroll ───────────────────────────────────────────────────────────────

    public interface IPayrollRepository
    {
        Task<Payroll> GeneratePayrollAsync(Payroll payroll);
        Task<List<Payroll>> GetPayrollsAsync(Guid? employeeId, int? month, int? year);
        Task<Payroll?> GetPayrollByIdAsync(Guid id);
        Task<Payroll?> UpdatePayrollStatusAsync(Guid id, string status);
    }

    public class PayrollRepository : IPayrollRepository
    {
        private readonly HrDbContext _db;
        private readonly IAttendanceRepository _attendance;

        public PayrollRepository(HrDbContext db, IAttendanceRepository attendance)
        {
            _db = db;
            _attendance = attendance;
        }

        public async Task<Payroll> GeneratePayrollAsync(Payroll payroll)
        {
            var monthStart = new DateTime(payroll.Year, payroll.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var records = await _attendance.GetAttendancesAsync(payroll.EmployeeId, monthStart, monthEnd);

            payroll.WorkingDays = (int)(monthEnd - monthStart).TotalDays + 1;
            payroll.PresentDays = records.Count(r => r.Status == "Present" || r.Status == "HalfDay");
            payroll.LeaveDays = records.Count(r => r.Status == "OnLeave");

            var gross = payroll.BasicSalary + payroll.Allowances + payroll.Bonuses + payroll.OvertimePay;
            payroll.TaxAmount = Math.Round(gross * 0.10m, 2);
            payroll.NetSalary = gross - payroll.Deductions - payroll.TaxAmount;

            _db.Payrolls.Add(payroll);
            await _db.SaveChangesAsync();
            return payroll;
        }

        public async Task<List<Payroll>> GetPayrollsAsync(Guid? employeeId, int? month, int? year)
        {
            var query = _db.Payrolls.Include(p => p.Employee).AsQueryable();
            if (employeeId.HasValue) query = query.Where(p => p.EmployeeId == employeeId.Value);
            if (month.HasValue) query = query.Where(p => p.Month == month.Value);
            if (year.HasValue) query = query.Where(p => p.Year == year.Value);
            return await query.OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ToListAsync();
        }

        public async Task<Payroll?> GetPayrollByIdAsync(Guid id)
            => await _db.Payrolls.Include(p => p.Employee).FirstOrDefaultAsync(p => p.Id == id);

        public async Task<Payroll?> UpdatePayrollStatusAsync(Guid id, string status)
        {
            var payroll = await _db.Payrolls.FindAsync(id);
            if (payroll == null) return null;
            payroll.Status = status;
            if (status == "Paid") payroll.PaidAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return payroll;
        }
    }

    // ── Performance ───────────────────────────────────────────────────────────

    public interface IPerformanceRepository
    {
        Task<PerformanceReview> CreateReviewAsync(PerformanceReview review);
        Task<List<PerformanceReview>> GetReviewsAsync(Guid? employeeId, string? period);
        Task<PerformanceReview?> GetReviewByIdAsync(Guid id);
        Task<PerformanceReview?> UpdateStatusAsync(Guid id, string status);
    }

    public class PerformanceRepository : IPerformanceRepository
    {
        private readonly HrDbContext _db;
        public PerformanceRepository(HrDbContext db) => _db = db;

        public async Task<PerformanceReview> CreateReviewAsync(PerformanceReview review)
        {
            _db.PerformanceReviews.Add(review);
            await _db.SaveChangesAsync();
            return review;
        }

        public async Task<List<PerformanceReview>> GetReviewsAsync(Guid? employeeId, string? period)
        {
            var query = _db.PerformanceReviews
                .Include(pr => pr.Employee).Include(pr => pr.Reviewer).AsQueryable();
            if (employeeId.HasValue) query = query.Where(pr => pr.EmployeeId == employeeId.Value);
            if (!string.IsNullOrWhiteSpace(period)) query = query.Where(pr => pr.Period == period);
            return await query.OrderByDescending(pr => pr.ReviewDate).ToListAsync();
        }

        public async Task<PerformanceReview?> GetReviewByIdAsync(Guid id)
            => await _db.PerformanceReviews
                .Include(pr => pr.Employee).Include(pr => pr.Reviewer)
                .FirstOrDefaultAsync(pr => pr.Id == id);

        public async Task<PerformanceReview?> UpdateStatusAsync(Guid id, string status)
        {
            var pr = await _db.PerformanceReviews.FindAsync(id);
            if (pr == null) return null;
            pr.Status = status;
            await _db.SaveChangesAsync();
            return pr;
        }
    }

    // ── Document ──────────────────────────────────────────────────────────────

    public interface IDocumentRepository
    {
        Task<EmployeeDocument> UploadDocumentAsync(EmployeeDocument doc);
        Task<List<EmployeeDocument>> GetDocumentsByEmployeeAsync(Guid employeeId);
        Task<bool> DeleteDocumentAsync(Guid id);
    }

    public class DocumentRepository : IDocumentRepository
    {
        private readonly HrDbContext _db;
        public DocumentRepository(HrDbContext db) => _db = db;

        public async Task<EmployeeDocument> UploadDocumentAsync(EmployeeDocument doc)
        {
            _db.EmployeeDocuments.Add(doc);
            await _db.SaveChangesAsync();
            return doc;
        }

        public async Task<List<EmployeeDocument>> GetDocumentsByEmployeeAsync(Guid employeeId)
            => await _db.EmployeeDocuments.Include(d => d.Employee)
                .Where(d => d.EmployeeId == employeeId)
                .OrderByDescending(d => d.UploadedAt).ToListAsync();

        public async Task<bool> DeleteDocumentAsync(Guid id)
        {
            var doc = await _db.EmployeeDocuments.FindAsync(id);
            if (doc == null) return false;
            _db.EmployeeDocuments.Remove(doc);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}