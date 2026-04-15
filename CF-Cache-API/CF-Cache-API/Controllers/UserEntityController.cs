using CF_Cache_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace CF_Cache_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserEntityController : ControllerBase
{
    private readonly UserEntityService _userEntityService;

    public UserEntityController(UserEntityService userEntityService)
    {
        _userEntityService = userEntityService;
    }

    [HttpGet("search/by-email")]
    public IActionResult GetByEmail([FromQuery] string email)
    {
        if (string.IsNullOrEmpty(email))
            return BadRequest(new { message = "Email is required" });

        var entities = _userEntityService.GetEntitiesByEmail(email);
        return Ok(new { email, entities, count = entities.Count });
    }

    [HttpGet("search/by-tenant/{tenantId}")]
    public IActionResult GetByTenant([FromRoute] string tenantId)
    {
        var entities = _userEntityService.GetEntitiesByTenant(tenantId);
        return Ok(new { tenantId, entities, count = entities.Count });
    }
}
