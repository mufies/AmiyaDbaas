using AmiyaDbaasManager.DTOs.Response.DbInstance;

namespace AmiyaDbaasManager.Services.Interfaces
{
    public interface IDbInstanceService
    {
        public Task<List<DbInstanceResponseDto>> GetAll();
        public Task<List<DbInstanceResponseDto>> GetByUser(string UserId);
        public Task<DbInstanceResponseDto?> UpdateStatus(Guid instanceId, string status);
    }
}
