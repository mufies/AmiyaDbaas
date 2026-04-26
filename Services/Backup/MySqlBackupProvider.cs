using AmiyaDbaasManager.Enums;

namespace AmiyaDbaasManager.Services.Backup
{
    public class MySqlBackupProvider : IBackupProvider
    {
        public IEnumerable<string> CreateBackupCmd(string instanceName, string date) =>
            new[]
            {
                "mysqldump",
                $"-u{DbEngineConfig.DefaultUser.MySQL}",
                "--single-transaction",
                "--routines",
                "--trigger",
                DbEngineConfig.DefaultDatabase.dbName,
                ">",
                DbEngineConfig.ContainerFilePath.MySQL(instanceName, date),
            };

        public IEnumerable<string> RestoreDataCmd(string filePath) =>
            new[]
            {
                "mysql",
                $"-u{DbEngineConfig.DefaultUser.MySQL}",
                DbEngineConfig.DefaultDatabase.dbName,
                "<",
                filePath,
            };

        public IEnumerable<string> GetEnvVars(string password) => new[] { $"MYSQL_PWD={password}" };
    }
}
