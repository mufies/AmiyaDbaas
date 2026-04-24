namespace AmiyaDbaasManager.Exceptions;

/// <summary>
/// Base custom exception. Mọi domain exception nên kế thừa từ class này.
/// </summary>
public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}
