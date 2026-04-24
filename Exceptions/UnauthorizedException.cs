namespace AmiyaDbaasManager.Exceptions;

/// <summary>
/// Throw khi user không được phép truy cập → 401 Unauthorized
/// </summary>
public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Unauthorized")
        : base(message, 401) { }
}
