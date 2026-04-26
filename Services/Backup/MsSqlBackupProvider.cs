using AmiyaDbaasManager.Enums;

namespace AmiyaDbaasManager.Services.Backup
{
    public class MsSqlBackupProvider : IBackupProvider
    {
        public IEnumerable<string> CreateBackupCmd(string instanceName, string date) =>
            new[]
            {
                "sqlcmd",
                "-Q",
                $"BACKUP DATABASE {DbEngineConfig.DefaultDatabase.dbName} TO DISK = '{DbEngineConfig.ContainerFilePath.MSSQL(instanceName, date)}'",
            };

        public IEnumerable<string> RestoreDataCmd(string filePath) =>
            new[]
            {
                "sqlcmd",
                "-Q",
                $"RESTORE DATABASE {DbEngineConfig.DefaultDatabase.dbName} FROM DISK = '{filePath}'",
            };

        public IEnumerable<string> GetEnvVars(string password) =>
            new[] { $"ACCEPT_EULA=Y", $"SA_PASSWORD={password}" };
    }
}
