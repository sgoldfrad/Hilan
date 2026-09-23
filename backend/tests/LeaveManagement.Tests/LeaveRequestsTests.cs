using LeaveManagement.Api.Controllers;
using LeaveManagement.Api.Models;
using LeaveManagement.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LeaveManagement.Tests;

public class LeaveRequestsTests : IClassFixture<TestDatabase>
{
    private readonly TestDatabase _database;

    public LeaveRequestsTests(TestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public void Create_WithinQuota_Succeeds()
    {
        // Arrange
        using var db = _database.NewDb();
        var emp = new Employee { Name = "Test Emp", AnnualQuota = 20 };
        db.Employees.Add(emp);
        db.SaveChanges();

        var controller = new LeaveRequestsController(new LeaveRequestService(db));

        // Act: request 3 days, well within the quota.
        var result = controller.Create(new CreateLeaveRequestDto
        {
            EmployeeId = emp.Id,
            Type = LeaveType.Vacation,
            StartDate = new DateTime(2026, 3, 1),
            EndDate = new DateTime(2026, 3, 3)
        });

        // Assert
        Assert.IsType<OkObjectResult>(result);
        Assert.Single(db.LeaveRequests);
    }

    [Fact]
    public void Create_ExceedingRemainingQuota_ReturnsBadRequest()
    {
        // Arrange
        using var db = _database.NewDb();
        var emp = new Employee { Name = "Test Emp", AnnualQuota = 10 };
        db.Employees.Add(emp);
        db.SaveChanges();

        // Employee already used 8 of their 10 days.
        db.LeaveRequests.Add(new LeaveRequest
        {
            EmployeeId = emp.Id,
            Type = LeaveType.Vacation,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 8),
            Days = 8,
            Status = LeaveStatus.Approved
        });
        db.SaveChanges();

        var controller = new LeaveRequestsController(new LeaveRequestService(db));

        // Act: request 3 more days, which would push the total to 11 (over the quota of 10).
        var result = controller.Create(new CreateLeaveRequestDto
        {
            EmployeeId = emp.Id,
            Type = LeaveType.Vacation,
            StartDate = new DateTime(2026, 3, 1),
            EndDate = new DateTime(2026, 3, 3)
        });

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Single(db.LeaveRequests);
    }
}
