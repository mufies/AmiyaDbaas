using AmiyaDbaasManager.DTOs;

namespace AmiyaDbaasManager.Services.Interfaces
{
    public interface IBackupService
    {
        Task CreateBackupForInstanceAsync(CreateBackupRequest request);
        Task RestoreBackupForInstanceAsync(string instanceId, string filePath);
    }
}
