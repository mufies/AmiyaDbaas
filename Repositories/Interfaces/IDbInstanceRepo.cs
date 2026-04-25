using AmiyaDbaasManager.Models;

namespace AmiyaDbaasManager.Repositories.Interfaces
{
    public interface IDbInstanceRepo
    {
        Task<DbInstance> Create(DbInstance instance);
        Task<List<int>> GetPortList();
        Task<List<DbInstance>> GetAll();
        Task<List<DbInstance>> GetDbInstanceOfUser(string userId);
        Task<DbInstance?> GetById(Guid id);
        Task<DbInstance?> UpdateStatus(Guid instanceId, string status);
        Task DeleteInstance(Guid id, string userId);
        Task UpdateRangeAsync(IEnumerable<DbInstance> instances);
        Task<List<DbInstance>> GetPagedAsync(int page, int pageSize);
    }
}
