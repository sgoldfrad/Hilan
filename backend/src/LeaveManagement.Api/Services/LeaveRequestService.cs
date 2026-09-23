using LeaveManagement.Api.Data;
using LeaveManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Services;

public enum LeaveRequestOperationStatus
{
    Success,
    EmployeeNotFound,
    QuotaExceeded,
    RequestNotFound,
    AlreadyProcessed
}

public record LeaveRequestOperationResult(
    LeaveRequestOperationStatus Status,
    LeaveRequest? Request = null,
    string? Error = null);

public interface ILeaveRequestService
{
    List<LeaveRequest> GetAll();
    List<LeaveRequest> Search(string name);
    LeaveRequestOperationResult Create(CreateLeaveRequestDto dto);
    Task<LeaveRequestOperationResult> ApproveAsync(int id);
}

public class LeaveRequestService : ILeaveRequestService
{
    private readonly LeaveDbContext _db;

    public LeaveRequestService(LeaveDbContext db)
    {
        _db = db;
    }

    public List<LeaveRequest> GetAll()
    {
        return _db.LeaveRequests
            .Include(r => r.Employee)
            .OrderByDescending(r => r.StartDate)
            .ToList();
    }

    // Lets the UI quickly find requests by employee name.
    public List<LeaveRequest> Search(string name)
    {
        return _db.LeaveRequests
            .Include(r => r.Employee)
            .Where(r => r.Employee != null && EF.Functions.Like(r.Employee.Name, $"%{name}%"))
            .ToList();
    }

    public LeaveRequestOperationResult Create(CreateLeaveRequestDto dto)
    {
        var employee = _db.Employees.FirstOrDefault(e => e.Id == dto.EmployeeId);
        if (employee == null)
            return new LeaveRequestOperationResult(LeaveRequestOperationStatus.EmployeeNotFound, Error: "Employee not found");

        var days = (dto.EndDate - dto.StartDate).Days + 1;

        // How many vacation days has the employee already used this year?
        var used = _db.LeaveRequests
            .Where(r => r.EmployeeId == dto.EmployeeId
                        && r.Type == LeaveType.Vacation
                        && r.Status == LeaveStatus.Approved)
            .Sum(r => r.Days);

        // Make sure the request does not exceed the quota, accounting for days already used.
        if (dto.Type == LeaveType.Vacation && used + days > employee.AnnualQuota)
        {
            return new LeaveRequestOperationResult(LeaveRequestOperationStatus.QuotaExceeded, Error: "Not enough vacation balance");
        }

        var request = new LeaveRequest
        {
            EmployeeId = dto.EmployeeId,
            Type = dto.Type,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Days = days,
            Status = LeaveStatus.Pending
        };

        _db.LeaveRequests.Add(request);
        _db.SaveChanges();

        return new LeaveRequestOperationResult(LeaveRequestOperationStatus.Success, request);
    }

    public async Task<LeaveRequestOperationResult> ApproveAsync(int id)
    {
        // Lock the row before reading it, so two concurrent approvals of the same
        // request serialize instead of both seeing "Pending" and both succeeding.
        // Postgres: real row-level lock. SQLite: no-op here (see DECISIONS.md).
        using var tx = await _db.Database.BeginTransactionAsync();

        if (_db.Database.IsNpgsql())
        {
            await _db.Database.ExecuteSqlRawAsync(
                "SELECT 1 FROM \"LeaveRequests\" WHERE \"Id\" = {0} FOR UPDATE", id);
        }

        var request = await _db.LeaveRequests.FirstOrDefaultAsync(r => r.Id == id);
        if (request == null)
            return new LeaveRequestOperationResult(LeaveRequestOperationStatus.RequestNotFound, Error: "Leave request not found");

        if (request.Status != LeaveStatus.Pending)
            return new LeaveRequestOperationResult(LeaveRequestOperationStatus.AlreadyProcessed, Error: $"Leave request is already {request.Status}");

        request.Status = LeaveStatus.Approved;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return new LeaveRequestOperationResult(LeaveRequestOperationStatus.Success, request);
    }
}
