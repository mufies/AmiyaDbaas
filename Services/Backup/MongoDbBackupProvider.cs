using AmiyaDbaasManager.Enums;

namespace AmiyaDbaasManager.Services.Backup
{
    public class MongoDbBackupProvider : IBackupProvider
    {
        public IEnumerable<string> CreateBackupCmd(string instanceName, string date) =>
            new[]
            {
                "mongodump",
                $"--uri=mongodb://{DbEngineConfig.DefaultUser.MongoDB}:<password>@localhost:27017/{DbEngineConfig.DefaultDatabase.dbName}",
                $"--out={DbEngineConfig.ContainerFilePath.MongoDB(instanceName, date)}",
            };

        public IEnumerable<string> RestoreDataCmd(string filePath) =>
            new[]
            {
                "mongorestore",
                $"--uri=mongodb://{DbEngineConfig.DefaultUser.MongoDB}:<password>@localhost:27017/{DbEngineConfig.DefaultDatabase.dbName}",
                $"--drop {filePath}",
            };

        public IEnumerable<string> GetEnvVars(string password) =>
            new[] { $"MONGO_INITDB_ROOT_PASSWORD={password}" };
    }
}
