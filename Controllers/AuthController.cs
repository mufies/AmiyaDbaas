using System.Security.Claims;
using AmiyaDbaasManager.DTOs.Request.Auth;
using AmiyaDbaasManager.DTOs.Response;
using AmiyaDbaasManager.DTOs.Response.Auth;
using AmiyaDbaasManager.Exceptions;
using AmiyaDbaasManager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmiyaDbaasManager.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var user = await _authService.RegisterAsync(request);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<UserResponseDto>.Ok(user, "Đăng ký tài khoản thành công."));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(ApiResponse<LoginResponseDto>.Ok(result, "Đăng nhập thành công."));
    }


    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        // ASP.NET Core maps JWT "sub" → ClaimTypes.NameIdentifier automatically
        var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedException("Token không hợp lệ.");

        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedException("Token không hợp lệ.");

        var user = await _authService.GetUserAsync(userId);
        return Ok(ApiResponse<UserResponseDto>.Ok(user, "Lấy thông tin user thành công."));
    }
}
