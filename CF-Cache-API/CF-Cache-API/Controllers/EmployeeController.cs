using CF_Cache_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace CF_Cache_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeeController : ControllerBase
{
    private readonly EmployeeService _employeeService;
    private readonly CloudFrontService _cloudFrontService;

    public EmployeeController(EmployeeService employeeService, CloudFrontService cloudFrontService)
    {
        _employeeService = employeeService;
        _cloudFrontService = cloudFrontService;
    }

    [HttpGet]
    public IActionResult GetAll([FromHeader(Name = "X-Tenant-Id")] string tenantId, [FromQuery] int page = 1, [FromQuery] int pageSize = 200)
    {
        if (string.IsNullOrEmpty(tenantId))
            return BadRequest(new { message = "X-Tenant-Id header is required" });

        var employees = _employeeService.GetAll(tenantId, page, pageSize);
        var total = _employeeService.GetTotalCount(tenantId);
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return Ok(new { data = employees, total, totalPages, page, pageSize });
    }

    [HttpGet("{id}")]
    public IActionResult GetById([FromHeader(Name = "X-Tenant-Id")] string tenantId, [FromRoute] int id)
    {
        if (string.IsNullOrEmpty(tenantId))
            return BadRequest(new { message = "X-Tenant-Id header is required" });

        var employee = _employeeService.GetAll(tenantId, 1, int.MaxValue).FirstOrDefault(e => e.Id == id);
        if (employee == null)
            return NotFound(new { message = "Employee not found" });

        return Ok(employee);
    }

    [HttpGet("search")]
    public IActionResult Search([FromHeader(Name = "X-Tenant-Id")] string tenantId, [FromQuery] string? firstName, [FromQuery] string? lastName, 
        [FromQuery] string? companyName, [FromQuery] string? position, [FromQuery] string? department,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (string.IsNullOrEmpty(tenantId))
            return BadRequest(new { message = "X-Tenant-Id header is required" });

        var query = _employeeService.GetAll(tenantId, 1, int.MaxValue).AsEnumerable();

        if (!string.IsNullOrEmpty(firstName))
            query = query.Where(e => e.FirstName.Contains(firstName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(lastName))
            query = query.Where(e => e.LastName.Contains(lastName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(companyName))
            query = query.Where(e => e.CompanyName.Contains(companyName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(position))
            query = query.Where(e => e.Position.Contains(position, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(department))
            query = query.Where(e => e.Department.Contains(department, StringComparison.OrdinalIgnoreCase));

        var total = query.Count();
        var employees = query.Skip((page - 1) * pageSize).Take(pageSize);
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

    [HttpGet("{tenantId}/by-department")]
    public IActionResult SearchByDepartment(
        [FromRoute] string tenantId,
        [FromHeader(Name = "X-Tenant-Id")] string headerTenantId,
        [FromQuery] string? firstName,
        [FromQuery] string? department,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (string.IsNullOrEmpty(headerTenantId))
            return BadRequest(new { message = "X-Tenant-Id header is required" });

        if (tenantId != headerTenantId)
            return BadRequest(new { message = "Path tenantId must match X-Tenant-Id header" });

        var (employees, total) = _employeeService.SearchByDepartment(tenantId, firstName, department, page, pageSize);
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return Ok(new { tenantId, data = employees, total, totalPages, page, pageSize });
    }

    [HttpPost]
    public IActionResult AddEmployee([FromHeader(Name = "X-Tenant-Id")] string tenantId, [FromBody] CreateEmployeeRequest request)
    {
        if (string.IsNullOrEmpty(tenantId))
            return BadRequest(new { message = "X-Tenant-Id header is required" });

        var employee = _employeeService.AddEmployee(tenantId, request.FirstName, request.LastName, request.CompanyName, request.Position);
        return Ok(new { message = "Success", data = employee });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEmployee(
        [FromHeader(Name = "X-Tenant-Id")] string tenantId,
        [FromRoute] int id,
        [FromBody] UpdateEmployeeRequest request)
    {
        if (string.IsNullOrEmpty(tenantId))
            return BadRequest(new { message = "X-Tenant-Id header is required" });

        var employee = _employeeService.UpdateEmployee(tenantId, id, request.FirstName, request.LastName, request.CompanyName, request.Position, request.Department);
        if (employee == null)
            return NotFound(new { message = "Employee not found" });

        try
        {
            await _cloudFrontService.InvalidateCacheAsync(id);
        }
        catch (Exception ex)
        {
            // Log but don't fail the update
            Console.WriteLine($"CloudFront invalidation failed: {ex.Message}");
        }

        return Ok(new { message = "Updated", data = employee });
    }
}

public class CreateEmployeeRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
}

public class UpdateEmployeeRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}
