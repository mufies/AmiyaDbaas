using AmiyaDbaasManager.DTOs.Request.DbInstance;
using AmiyaDbaasManager.DTOs.Response.DbInstance;

namespace AmiyaDbaasManager.Services.Interfaces
{
    public interface IQueryRunService {
        Task<QueryResponseDto> RunQueryAsync(CreateQueryRequestDto request);
    }
}