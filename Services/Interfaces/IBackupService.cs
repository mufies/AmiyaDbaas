using AmiyaDbaasManager.DTOs;

namespace AmiyaDbaasManager.Services.Interfaces
{
    public interface IBackupService
    {
        /// <summary>
        /// Tạo backup từ container và upload lên MinIO.
        /// </summary>
        Task CreateBackupForInstanceAsync(CreateBackupRequest request, Guid userId, CancellationToken ct = default);

        /// <summary>
        /// Restore backup từ MinIO vào container.
        /// </summary>
        Task RestoreBackupForInstanceAsync(Guid dbInstanceId, string backupObjectPath, Guid userId, CancellationToken ct = default);
    }
}
