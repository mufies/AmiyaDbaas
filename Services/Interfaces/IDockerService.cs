using AmiyaDbaasManager.DTOs.Request.DbInstance;
using AmiyaDbaasManager.DTOs.Response;
using AmiyaDbaasManager.DTOs.Response.DbInstance;

namespace AmiyaDbaasManager.Services.Interfaces;

public interface IDockerService
{
    Task<ApiResponse<DbInstanceResponseDto>> createDbImage(CreateDbInstanceRequestDto request);
    Task<ApiResponse<DbInstanceResponseDto>> StartInstanceAsync(Guid instanceId, string userId);
    Task<ApiResponse<DbInstanceResponseDto>> StopInstanceAsync(Guid instanceId, string userId);
    Task<ApiResponse<string>> DeleteInstanceAsync(Guid instanceId, string userId);
    Task ContainerHealthCheck();
    Task SendLogsToGroup(
        string containerId,
        Microsoft.AspNetCore.SignalR.IHubContext<AmiyaDbaasManager.Hubs.InstanceLogs> hubContext
    );
    Task ExecAsync(
        string containerId,
        IEnumerable<string> cmd,
        IEnumerable<string>? envVars,
        CancellationToken ct = default
    );
    Task GetFileStreamFromContainer(
        string containerId,
        string contentPath,
        Func<Stream, long, Task> action,
        CancellationToken ct = default
    );
    Task CopyToContainer(
        string containerId,
        Stream fileStream,
        string contentPath,
        CancellationToken ct = default
    );
}
