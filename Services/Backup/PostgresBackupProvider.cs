using AmiyaDbaasManager.Enums;

namespace AmiyaDbaasManager.Services.Backup
{
    public class PostgresBackupProvider : IBackupProvider
    {
        public IEnumerable<string> CreateBackupCmd(string instanceName, string date) =>
            new[]
            {
                "pg_dump",
                "-U",
                $"{DbEngineConfig.DefaultUser.PostgreSQL}",
                $"{DbEngineConfig.DefaultDatabase.dbName}",
                ">",
                DbEngineConfig.ContainerFilePath.PostgreSQL(instanceName, date),
            };

        public IEnumerable<string> RestoreDataCmd(string filePath) =>
            new[]
            {
                "psql",
                "-U",
                $"{DbEngineConfig.DefaultUser.PostgreSQL}",
                $"{DbEngineConfig.DefaultDatabase.dbName}",
                "<",
                filePath,
            };

        public IEnumerable<string> GetEnvVars(string password) =>
            new[] { $"PGPASSWORD={password}" };
    }
}
