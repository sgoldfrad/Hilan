using LeaveManagement.Api.Data;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

// Read-only lookup so the UI can offer an employee picker.
[ApiController]
[Route("api/employees")]
public class EmployeesController : ControllerBase
{
    private readonly LeaveDbContext _db;

    public EmployeesController(LeaveDbContext db)
    {
        _db = db;
    }

    // GET /api/employees
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_db.Employees.ToList());
    }
}
