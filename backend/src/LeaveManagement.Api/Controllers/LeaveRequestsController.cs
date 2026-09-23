using LeaveManagement.Api.Models;
using LeaveManagement.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

// Thin controller: maps HTTP requests to ILeaveRequestService calls and
// translates the resulting status into the appropriate response code.
[ApiController]
[Route("api/leave-requests")]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestService _service;

    public LeaveRequestsController(ILeaveRequestService service)
    {
        _service = service;
    }

    // GET /api/leave-requests
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_service.GetAll());
    }

    // GET /api/leave-requests/search?name=Dana
    [HttpGet("search")]
    public IActionResult Search([FromQuery] string name)
    {
        return Ok(_service.Search(name));
    }

    // POST /api/leave-requests
    [HttpPost]
    public IActionResult Create([FromBody] CreateLeaveRequestDto dto)
    {
        var result = _service.Create(dto);
        return result.Status switch
        {
            LeaveRequestOperationStatus.Success => Ok(result.Request),
            LeaveRequestOperationStatus.EmployeeNotFound => NotFound(result.Error),
            LeaveRequestOperationStatus.QuotaExceeded => BadRequest(result.Error),
            _ => Problem(result.Error)
        };
    }

    // POST /api/leave-requests/{id}/approve
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var result = await _service.ApproveAsync(id);
        return result.Status switch
        {
            LeaveRequestOperationStatus.Success => Ok(result.Request),
            LeaveRequestOperationStatus.RequestNotFound => NotFound(result.Error),
            LeaveRequestOperationStatus.AlreadyProcessed => Conflict(result.Error),
            _ => Problem(result.Error)
        };
    }
}
