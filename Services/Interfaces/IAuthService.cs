using AmiyaDbaasManager.DTOs.Request.Auth;
using AmiyaDbaasManager.DTOs.Response.Auth;

namespace AmiyaDbaasManager.Services.Interfaces;

public interface IAuthService
{
    Task<UserResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
    Task<UserResponseDto> GetUserAsync(Guid userId);
    Task<bool> isUserValid(Guid userId);
}
