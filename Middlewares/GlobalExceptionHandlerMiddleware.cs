using System.Text.Json;
using AmiyaDbaasManager.DTOs.Response;
using AmiyaDbaasManager.Exceptions;

namespace AmiyaDbaasManager.Middlewares;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        int statusCode;
        string message;
        List<string>? errors = null;

        switch (exception)
        {
            case AppException appEx:
                statusCode = appEx.StatusCode;
                message = appEx.Message;
                _logger.LogWarning(exception, "Domain exception: {Message}", appEx.Message);
                break;

            default:
                statusCode = StatusCodes.Status500InternalServerError;
                message = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.";
                errors = new List<string> { exception.Message };
                _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
                break;
        }

        var response = ApiResponse<object>.Fail(message, errors);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}
