using LeaveManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Data;

public class LeaveDbContext : DbContext
{
    public LeaveDbContext(DbContextOptions<LeaveDbContext> options) : base(options) { }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Leave dates are calendar days, not instants. SQLite has no date type
        // (it stores them as text), so this only applies to PostgreSQL.
        if (Database.IsNpgsql())
        {
            modelBuilder.Entity<LeaveRequest>().Property(r => r.StartDate).HasColumnType("date");
            modelBuilder.Entity<LeaveRequest>().Property(r => r.EndDate).HasColumnType("date");
        }
    }

    public static void Seed(LeaveDbContext db)
    {
        if (db.Employees.Any()) return;

        var dana = new Employee { Name = "Dana Levi", AnnualQuota = 20 };
        var yossi = new Employee { Name = "Yossi Cohen", AnnualQuota = 14 };
        db.Employees.AddRange(dana, yossi);
        db.SaveChanges();

        // Dana has already used 18 of her 20 days (approved).
        db.LeaveRequests.Add(new LeaveRequest
        {
            EmployeeId = dana.Id,
            Type = LeaveType.Vacation,
            StartDate = new DateTime(2026, 1, 6),
            EndDate = new DateTime(2026, 1, 23),
            Days = 18,
            Status = LeaveStatus.Approved
        });
        db.SaveChanges();
    }
}
