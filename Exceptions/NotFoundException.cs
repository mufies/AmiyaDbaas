namespace AmiyaDbaasManager.Exceptions;

/// <summary>
/// Throw khi resource không tìm thấy → 404 Not Found
/// </summary>
public class NotFoundException : AppException
{
    public NotFoundException(string message = "Resource not found")
        : base(message, 404) { }
}
