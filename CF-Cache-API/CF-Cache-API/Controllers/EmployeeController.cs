using CF_Cache_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace CF_Cache_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeeController : ControllerBase
{
    private readonly EmployeeService _employeeService;

    public EmployeeController(EmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    public IActionResult GetAll([FromHeader(Name = "X-Tenant-Id")] string tenantId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (string.IsNullOrEmpty(tenantId))
            return BadRequest(new { message = "X-Tenant-Id header is required" });

        var employees = _employeeService.GetAll(tenantId, page, pageSize);
        var total = _employeeService.GetTotalCount(tenantId);
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return Ok(new { data = employees, total, totalPages, page, pageSize });
    }

    [HttpGet("search")]
    public IActionResult Search([FromHeader(Name = "X-Tenant-Id")] string tenantId, [FromQuery] string? firstName, [FromQuery] string? lastName, 
        [FromQuery] string? companyName, [FromQuery] string? position, 
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (string.IsNullOrEmpty(tenantId))
            return BadRequest(new { message = "X-Tenant-Id header is required" });

        var (employees, total) = _employeeService.Search(tenantId, firstName, lastName, companyName, position, page, pageSize);
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return Ok(new { data = employees, total, totalPages, page, pageSize });
    }

    [HttpGet("by-firstname")]
    public IActionResult GetByFirstName([FromHeader(Name = "X-Tenant-Id")] string tenantId, [FromQuery] string firstName, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (string.IsNullOrEmpty(tenantId))
            return BadRequest(new { message = "X-Tenant-Id header is required" });

        var (employees, total) = _employeeService.SearchByFirstName(tenantId, firstName, page, pageSize);
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return Ok(new { data = employees, total, totalPages, page, pageSize });
    }

    [HttpGet("by-lastname")]
    public IActionResult GetByLastName([FromHeader(Name = "X-Tenant-Id")] string tenantId, [FromQuery] string lastName, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (string.IsNullOrEmpty(tenantId))
            return BadRequest(new { message = "X-Tenant-Id header is required" });

        var (employees, total) = _employeeService.SearchByLastName(tenantId, lastName, page, pageSize);
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return Ok(new { data = employees, total, totalPages, page, pageSize });
    }

    [HttpGet("by-company")]
    public IActionResult GetByCompany([FromHeader(Name = "X-Tenant-Id")] string tenantId, [FromQuery] string companyName, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (string.IsNullOrEmpty(tenantId))
            return BadRequest(new { message = "X-Tenant-Id header is required" });

        var (employees, total) = _employeeService.SearchByCompany(tenantId, companyName, page, pageSize);
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return Ok(new { data = employees, total, totalPages, page, pageSize });
    }

    [HttpGet("by-position")]
    public IActionResult GetByPosition([FromHeader(Name = "X-Tenant-Id")] string tenantId, [FromQuery] string position, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (string.IsNullOrEmpty(tenantId))
            return BadRequest(new { message = "X-Tenant-Id header is required" });

        var (employees, total) = _employeeService.SearchByPosition(tenantId, position, page, pageSize);
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return Ok(new { data = employees, total, totalPages, page, pageSize });
    }

    [HttpPost]
    public IActionResult AddEmployee([FromHeader(Name = "X-Tenant-Id")] string tenantId, [FromBody] CreateEmployeeRequest request)
    {
        if (string.IsNullOrEmpty(tenantId))
            return BadRequest(new { message = "X-Tenant-Id header is required" });

        var employee = _employeeService.AddEmployee(tenantId, request.FirstName, request.LastName, request.CompanyName, request.Position);
        return Ok(new { message = "Success", data = employee });
    }
}

public class CreateEmployeeRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
}
