using AmiyaDbaasManager.DTOs;
using AmiyaDbaasManager.Enums;
using AmiyaDbaasManager.Repositories.Interfaces;
using AmiyaDbaasManager.Services.Interfaces;

namespace AmiyaDbaasManager.Services
{
    public class BackupService : IBackupService
    {
        private readonly IDockerService _dockerService;
        private readonly IMinioService _minioService;
        private readonly IDbInstanceRepo _dbInstanceRepo;
        private readonly IEncryptionService _encryptionService;

        public BackupService(
            IDockerService dockerService,
            IMinioService minioService,
            IDbInstanceRepo dbInstanceRepo,
            IEncryptionService encryptionService
        )
        {
            _dockerService = dockerService;
            _minioService = minioService;
            _dbInstanceRepo = dbInstanceRepo;
            _encryptionService = encryptionService;
        }

        // CREATE BACKUP
        // Flow: ExecAsync (dump inside container) → GetFileStreamFromContainer
        //       → UploadStreamFileToMinio → ExecAsync (cleanup)
        public async Task CreateBackupForInstanceAsync(
            CreateBackupRequest request,
            Guid userId,
            CancellationToken ct = default
        )
        {
            var instance =
                await _dbInstanceRepo.GetById(request.DbInstanceId)
                ?? throw new KeyNotFoundException("Instance không tồn tại.");

            if (instance.UserId != userId)
                throw new UnauthorizedAccessException("Bạn không có quyền backup instance này.");

            if (instance.Status != "Running")
                throw new InvalidOperationException(
                    "Instance phải đang ở trạng thái Running để thực hiện backup."
                );

            string containerId = instance.DockerContainerId;
            string rawPassword = _encryptionService.Decrypt(instance.Password);
            DbEngine engine = DbEngineConfig.ParseEngine(instance.Engine);

            string date = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string containerDumpPath = BackupCommands.ContainerFilePath.Get(
                engine,
                instance.InstanceName,
                date
            );
            IEnumerable<string> dumpCmd = BackupCommands.GetBackupCmd(
                engine,
                instance.InstanceName,
                date,
                rawPassword
            );
            IEnumerable<string> envVars = BackupCommands.GetEnvVars(engine, rawPassword);

            string dumpDir = Path.GetDirectoryName(containerDumpPath)!.Replace("\\", "/");
            await _dockerService.ExecAsync(containerId, new[] { "mkdir", "-p", dumpDir }, null, ct);

            await _dockerService.ExecAsync(containerId, dumpCmd, envVars, ct);

            string minioObjectPath = $"backups/{userId}/{instance.Id}/{date}.dump";

            await _minioService.EnsureBucketExistsAsync();

            await _dockerService.GetFileStreamFromContainer(
                containerId,
                containerDumpPath,
                async (stream, length) =>
                {
                    await _minioService.UploadStreamFileToMinio(
                        stream,
                        length,
                        minioObjectPath,
                        ct
                    );
                },
                ct
            );

            await _dockerService.ExecAsync(
                containerId,
                new[] { "rm", "-rf", dumpDir },
                envVars: null,
                ct
            );
        }

        // RESTORE BACKUP
        // Flow: GetStreamFileFromMinio → CopyToContainer → ExecAsync (restore)
        public async Task RestoreBackupForInstanceAsync(
            Guid dbInstanceId,
            string backupObjectPath,
            Guid userId,
            CancellationToken ct = default
        )
        {
            var instance =
                await _dbInstanceRepo.GetById(dbInstanceId)
                ?? throw new KeyNotFoundException("Instance không tồn tại.");

            if (instance.UserId != userId)
                throw new UnauthorizedAccessException("Bạn không có quyền restore instance này.");

            if (instance.Status != "Running")
                throw new InvalidOperationException(
                    "Instance phải đang ở trạng thái Running để thực hiện restore."
                );

            string containerId = instance.DockerContainerId;
            string rawPassword = _encryptionService.Decrypt(instance.Password);
            DbEngine engine = DbEngineConfig.ParseEngine(instance.Engine);

            string date = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string containerDumpPath = BackupCommands.ContainerFilePath.Get(
                engine,
                instance.InstanceName,
                date
            );
            IEnumerable<string> envVars = BackupCommands.GetEnvVars(engine, rawPassword);

            // Tạo thư mục trước khi copy file vào container để tránh lỗi "Directory nonexistent"
            string dumpDir = Path.GetDirectoryName(containerDumpPath)!.Replace("\\", "/");
            await _dockerService.ExecAsync(containerId, new[] { "mkdir", "-p", dumpDir }, null, ct);

            await _minioService.GetStreamFileFromMinio(
                backupObjectPath,
                async (minioStream) =>
                {
                    using var ms = new MemoryStream();
                    await minioStream.CopyToAsync(ms, ct);
                    ms.Position = 0;

                    await _dockerService.CopyToContainer(containerId, ms, containerDumpPath, ct);
                },
                ct
            );

            IEnumerable<string> restoreCmd = BackupCommands.GetRestoreCmd(
                engine,
                containerDumpPath,
                rawPassword
            );
            await _dockerService.ExecAsync(containerId, restoreCmd, envVars, ct);

            await _dockerService.ExecAsync(
                containerId,
                new[] { "rm", "-rf", dumpDir },
                envVars: null,
                ct
            );
        }
    }
}
