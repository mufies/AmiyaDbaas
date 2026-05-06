using AmiyaDbaasManager.DTOs.Response;

namespace AmiyaDbaasManager.Services.Interfaces
{
    public interface IMinioService
    {
        Task UploadStreamFileToMinio(
            Stream file,
            long length,
            string filePath,
            CancellationToken ct
        );
        Task GetStreamFileFromMinio(
            string filePath,
            Func<Stream, Task> act,
            CancellationToken ct = default
        );
        Task EnsureBucketExistsAsync();
        Task<List<BackupResponseDto>> ListBackupsAsync(
            Guid userId,
            Guid? instanceId = null,
            CancellationToken ct = default
        );
    }
}
