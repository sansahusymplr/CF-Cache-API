using CF_Cache_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace CF_Cache_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;
    private readonly TenantCtxService _tenantCtxService;

    public AuthController(UserService userService, TenantCtxService tenantCtxService)
    {
        _userService = userService;
        _tenantCtxService = tenantCtxService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = _userService.Authenticate(request.Email, request.Password);
        
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        // Mint signed TenantCtx cookie
        var tenantCtxValue = await _tenantCtxService.MintTenantCtxAsync(user.TenantId, ttlMinutes: 60);
        
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
            message = "Login successful"
        });
    }

    [HttpPost("logout")]
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

    [HttpGet("users")]
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
