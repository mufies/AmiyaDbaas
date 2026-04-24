namespace AmiyaDbaasManager.Exceptions;

/// <summary>
/// Throw khi dữ liệu bị xung đột (e.g. email đã tồn tại) → 409 Conflict
/// </summary>
public class ConflictException : AppException
{
    public ConflictException(string message = "Conflict")
        : base(message, 409) { }
}
