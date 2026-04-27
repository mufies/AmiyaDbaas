using AmiyaDbaasManager.DTOs;
using AmiyaDbaasManager.Services.Interfaces;

namespace AmiyaDbaasManager.Services
{
    public class BackupService : IBackupService
    {
        private readonly IDockerService _dockerService;

        public BackupService(IDockerService dockerService)
        {
            _dockerService = dockerService;
        }

        public async Task CreateBackupForInstanceAsync(CreateBackupRequest request) { }

        public async Task RestoreBackupForInstanceAsync(string instanceId, string filePath) { }
    }
}
