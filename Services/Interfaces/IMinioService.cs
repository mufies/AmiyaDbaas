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
    }
}
