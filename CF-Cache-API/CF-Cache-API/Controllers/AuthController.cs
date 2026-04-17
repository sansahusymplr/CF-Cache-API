using CF_Cache_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace CF_Cache_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;
    private readonly TenantCtxService _tenantCtxService;
    private readonly UserEntityService _userEntityService;
    private readonly KeyService _keyService;

    public AuthController(UserService userService, TenantCtxService tenantCtxService, UserEntityService userEntityService, KeyService keyService)
    {
        _userService = userService;
        _tenantCtxService = tenantCtxService;
        _userEntityService = userEntityService;
        _keyService = keyService;
    }

    [HttpPost("upsert/login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = _userService.Authenticate(request.Email, request.Password);
        
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        // Lookup entities from user-entity mapping
        var entities = _userEntityService.GetEntityNamesForUser(user.Email);

        // Generate per-user signing key via KMS and store in DynamoDB
        var (kid, signingKey) = await _keyService.GenerateAndStoreKeyAsync(user.Email);

        // Mint signed TenantCtx cookie with per-user key
        var entityCsv = string.Join(",", entities);
        // var clientIp = Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
        //                ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
        var tenantCtxValue = _tenantCtxService.MintTenantCtx(user.TenantId, signingKey, kid, entityCsv, ttlMinutes: 60);
        
        var domain = Request.Host.Host;
        
        Response.Cookies.Append("TenantCtx", tenantCtxValue, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/",
            Domain = domain.Contains("cloudfront.net") ? domain : null,
            Expires = DateTimeOffset.UtcNow.AddMinutes(60)
        });

        return Ok(new 
        { 
            email = user.Email, 
            tenantId = user.TenantId,
            entities,
            message = "Login successful"
        });
    }

    [HttpPost("upsert/logout")]
    public IActionResult Logout()
    {
        var domain = Request.Host.Host;
        
        Response.Cookies.Delete("TenantCtx", new CookieOptions
        {
            Path = "/",
            Domain = domain.Contains("cloudfront.net") ? domain : null
        });
        
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpGet("search/users")]
    public IActionResult GetUsers()
    {
        var users = _userService.GetAllUsers();
        return Ok(users);
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
