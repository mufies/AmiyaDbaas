using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AmiyaDbaasManager.DTOs.Request.Auth;
using AmiyaDbaasManager.DTOs.Response.Auth;
using AmiyaDbaasManager.Exceptions;
using AmiyaDbaasManager.Models;
using AmiyaDbaasManager.Repositories.Interfaces;
using AmiyaDbaasManager.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace AmiyaDbaasManager.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public AuthService(IUserRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    public async Task<UserResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        // Kiểm tra email đã tồn tại chưa
        if (await _userRepository.ExistsByEmailAsync(request.Email))
            throw new ConflictException($"Email '{request.Email}' đã được sử dụng.");

        // Kiểm tra username đã tồn tại chưa
        if (await _userRepository.ExistsByUsernameAsync(request.Username))
            throw new ConflictException($"Username '{request.Username}' đã được sử dụng.");

        var user = new User
        {
            Username = request.Username.Trim(),
            Email = request.Email.Trim().ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow,
        };

        var createdUser = await _userRepository.CreateAsync(user);
        return MapToUserResponse(createdUser);
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user =
            await _userRepository.GetByEmailAsync(request.Email)
            ?? throw new UnauthorizedException("Email hoặc mật khẩu không đúng.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Email hoặc mật khẩu không đúng.");

        var (token, expiresAt) = GenerateJwtToken(user);

        return new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = MapToUserResponse(user),
        };
    }

    public async Task<UserResponseDto> GetUserAsync(Guid userId)
    {
        var user =
            await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException($"Không tìm thấy user với ID: {userId}");

        return MapToUserResponse(user);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────────

    private (string token, DateTime expiresAt) GenerateJwtToken(User user)
    {
        var jwtKey =
            _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key chưa được cấu hình.");
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public async Task<bool> isUserValid(Guid user)
    {
        var exist = await _userRepository.GetByIdAsync(user);
        if (exist != null)
            return true;
        return false;
    }

    private static UserResponseDto MapToUserResponse(User user) =>
        new()
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
        };
}
