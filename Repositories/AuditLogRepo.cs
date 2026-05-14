using AmiyaDbaasManager.Data;
using AmiyaDbaasManager.DTOs.Request.AuditLog;
using AmiyaDbaasManager.Models;
using AmiyaDbaasManager.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AmiyaDbaasManager.Repositories
{
    public class AuditLogRepo : IAuditLogRepo
    {
        private readonly AppDbContext _dbcontext;

        public AuditLogRepo(AppDbContext dbContext)
        {
            _dbcontext = dbContext;
        }

        public async Task<List<AuditLog>> GetInstanceLogs(
            GetAuditLogsRequestDto getAuditLogsRequestDto
        )
        {
            return await _dbcontext
                .AuditLogs.Where(u => u.InstanceId.Equals(getAuditLogsRequestDto.InstanceId))
                .ToListAsync();
        }

        public async Task<AuditLog> AddLogToInstance(AuditLog auditLog)
        {
            await _dbcontext.AuditLogs.AddAsync(auditLog);
            await _dbcontext.SaveChangesAsync();
            return auditLog;
        }
    }
}
