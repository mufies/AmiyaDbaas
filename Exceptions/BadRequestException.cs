namespace AmiyaDbaasManager.Exceptions;

/// <summary>
/// Throw khi input không hợp lệ ở tầng business logic → 400 Bad Request
/// </summary>
public class BadRequestException : AppException
{
    public BadRequestException(string message = "Bad request")
        : base(message, 400) { }
}
