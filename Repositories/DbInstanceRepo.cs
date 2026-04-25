using AmiyaDbaasManager.Data;
using AmiyaDbaasManager.Models;
using AmiyaDbaasManager.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AmiyaDbaasManager.Repositories;

public class DbInstanceRepo : IDbInstanceRepo
{
    private readonly AppDbContext _dbcontext;

    public DbInstanceRepo(AppDbContext dbcontext)
    {
        _dbcontext = dbcontext;
    }

    public AppDbContext Dbcontext => _dbcontext;

    public async Task<DbInstance> Create(DbInstance instance)
    {
        await _dbcontext.DbInstances.AddAsync(instance);
        await _dbcontext.SaveChangesAsync();
        return instance;
    }

    public async Task<List<int>> GetPortList()
    {
        return await _dbcontext.DbInstances.Select(u => u.AllocatedPort).ToListAsync();
    }

    public async Task<List<DbInstance>> GetAll()
    {
        return await _dbcontext.DbInstances.ToListAsync();
    }

    public async Task<List<DbInstance>> GetDbInstanceOfUser(string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid))
            return new List<DbInstance>();
        return await _dbcontext.DbInstances.Where(u => u.UserId == userGuid).ToListAsync();
    }

    public async Task<DbInstance?> GetById(Guid id)
    {
        return await _dbcontext.DbInstances.FindAsync(id);
    }

    public async Task<DbInstance?> UpdateStatus(Guid instanceId, string status)
    {
        var exist = await _dbcontext.DbInstances.FindAsync(instanceId);
        if (exist != null)
        {
            exist.Status = status;
            await _dbcontext.SaveChangesAsync();
        }
        return exist;
    }

    public async Task DeleteInstance(Guid instanceId, string userId)
    {
        var exist = await _dbcontext.DbInstances.FindAsync(instanceId);
        if (exist != null)
        {
            _dbcontext.DbInstances.Remove(exist);
            await _dbcontext.SaveChangesAsync();
        }
    }

    public async Task UpdateRangeAsync(IEnumerable<DbInstance> instances)
    {
        _dbcontext.DbInstances.UpdateRange(instances);
        await _dbcontext.SaveChangesAsync();
    }

    public async Task<List<DbInstance>> GetPagedAsync(int page, int pageSize)
    {
        return await _dbcontext.DbInstances
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}
