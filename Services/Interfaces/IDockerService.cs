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
}
