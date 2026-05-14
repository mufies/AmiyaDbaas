using AmiyaDbaasManager.DTOs.Request.AuditLog;
using AmiyaDbaasManager.DTOs.Response;
using AmiyaDbaasManager.Models;

namespace AmiyaDbaasManager.Repositories.Interfaces
{
    public interface IAuditLogRepo
    {
        Task<List<AuditLog>> GetInstanceLogs(GetAuditLogsRequestDto getAuditLogsRequestDto);
        Task<AuditLog> AddLogToInstance(AuditLog auditLog);
    }
}
