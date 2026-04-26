using AmiyaDbaasManager.DTOs.Request.DbInstance;
using AmiyaDbaasManager.DTOs.Response;
using AmiyaDbaasManager.DTOs.Response.DbInstance;

namespace AmiyaDbaasManager.Services.interfaces;

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
    Task<Stream> GetFileStreamFromContainer(
        string containerId,
        string contentPath,
        CancellationToken ct = default
    );
    Task CopyToContainer(
        string containerId,
        Stream fileStream,
        string contentPath,
        CancellationToken ct = default
    );
}
