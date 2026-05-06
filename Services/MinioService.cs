using AmiyaDbaasManager.Services.Interfaces;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using AmiyaDbaasManager.DTOs.Response;

namespace AmiyaDbaasManager.Services
{
    public class MinioService : IMinioService
    {
        private readonly IMinioClient _minioClient;
        private readonly string _bucketName;

        public MinioService(IMinioClient minioClient, IConfiguration configuration)
        {
            _minioClient = minioClient;
            _bucketName = configuration["Minio:BucketName"];
        }

        public async Task EnsureBucketExistsAsync()
        {
            var exists = await _minioClient.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_bucketName)
            );

            if (!exists)
            {
                await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));
            }
        }

        public async Task UploadStreamFileToMinio(
            Stream file,
            long length,
            string filePath,
            CancellationToken ct = default
        )
        {
            await _minioClient.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(filePath)
                    .WithStreamData(file)
                    .WithObjectSize(length)
                    .WithContentType("application/otect-stream"),
                ct
            );
        }

        public async Task GetStreamFileFromMinio(
            string filePath,
            Func<Stream, Task> act,
            CancellationToken ct = default
        )
        {
            await _minioClient.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(filePath)
                    .WithCallbackStream(async stream =>
                    {
                        await act(stream);
                    }),
                ct
            );
        }

        public async Task<List<BackupResponseDto>> ListBackupsAsync(
            Guid userId,
            Guid? instanceId = null,
            CancellationToken ct = default
        )
        {
            var prefix = instanceId.HasValue
                ? $"backups/{userId}/{instanceId}/"
                : $"backups/{userId}/";

            var files = new List<BackupResponseDto>();

            var args = new ListObjectsArgs()
                .WithBucket(_bucketName)
                .WithPrefix(prefix)
                .WithRecursive(true);

            await foreach (var item in _minioClient.ListObjectsEnumAsync(args, ct))
            {
                files.Add(new BackupResponseDto
                {
                    BackupPath = item.Key,
                    FileName = Path.GetFileName(item.Key)
                });
            }

            return files;
        }
    }
}
