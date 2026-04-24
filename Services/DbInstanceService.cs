using AmiyaDbaasManager.DTOs.Response.DbInstance;
using AmiyaDbaasManager.Mappers;
using AmiyaDbaasManager.Repositories.Interfaces;
using AmiyaDbaasManager.Services.Interfaces;

namespace AmiyaDbaasManager.Services
{
    public class DbInstanceService : IDbInstanceService
    {
        private readonly IDbInstanceRepo _dbInstanceRepo;

        public DbInstanceService(IDbInstanceRepo dbInstanceRepo)
        {
            _dbInstanceRepo = dbInstanceRepo;
        }

        public async Task<List<DbInstanceResponseDto>> GetAll()
        {
            var instances = await _dbInstanceRepo.GetAll();
            return instances.ToDtoList();
        }

        public async Task<List<DbInstanceResponseDto>> GetByUser(string UserId)
        {
            var instances = await _dbInstanceRepo.GetDbInstanceOfUser(UserId);
            return instances.ToDtoList();
        }

        public async Task<DbInstanceResponseDto?> UpdateStatus(Guid instanceId, string status)
        {
            var instance = await _dbInstanceRepo.UpdateStatus(instanceId, status);
            return instance?.ToDto();
        }
    }
}
