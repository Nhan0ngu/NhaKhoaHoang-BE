
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NhaKhoaHoang.API.DTOs;
using NhaKhoaHoang.API.Services;

namespace NhaKhoaHoang.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AdminAuthService _adminAuthService;

    public AuthController(AdminAuthService adminAuthService)
    {
        _adminAuthService = adminAuthService;
    }

    // POST /api/auth/login
    [AllowAnonymous]
    [EnableRateLimiting("admin-login")]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] AdminLoginRequest request)
    {
        var result = await _adminAuthService.LoginAsync(request);

        if (result == null)
        {
            return Unauthorized(new
            {
                success = false,
                message =
                    "Tên đăng nhập hoặc mật khẩu không đúng."
            });
        }

        return Ok(result);
    }

    // GET /api/auth/me
    [Authorize(Roles = "Admin")]
    [HttpGet("me")]
    public IActionResult GetCurrentAdmin()
    {
        return Ok(new
        {
            success = true,
            adminId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier
            )?.Value,
            username = User.Identity?.Name,
            role = "Admin"
        });
    }
}
