namespace AmiyaDbaasManager.Enums;

public static class BackupCommands
{
    public static class ContainerFilePath
    {
        public static string MySQL(string instanceName, string date) =>
            $"/tempfiles/mysql/{instanceName}_{date}/backup_{date}.sql";

        public static string PostgreSQL(string instanceName, string date) =>
            $"/tempfiles/postgresql/{instanceName}_{date}/backup_{date}.sql";

        public static string MSSQL(string instanceName, string date) =>
            $"/tempfiles/mssql/{instanceName}_{date}/backup_{date}.sql";

        public static string MongoDB(string instanceName, string date) =>
            $"/tempfiles/mongodb/{instanceName}_{date}/backup_{date}.sql";
    }

    public static class MySql
    {
        public static IEnumerable<string> CreateBackup(string instanceName, string date) =>
            new[]
            {
                "mysqldump",
                $"-u{DbEngineConfig.DefaultUser.MySQL}",
                "--single-transaction",
                "--routines",
                "--trigger",
                DbEngineConfig.DefaultDatabase.dbName,
                ">",
                ContainerFilePath.MySQL(instanceName, date),
            };

        public static IEnumerable<string> Restore(string filePath) =>
            new[]
            {
                "mysql",
                $"-u{DbEngineConfig.DefaultUser.MySQL}",
                DbEngineConfig.DefaultDatabase.dbName,
                "<",
                filePath,
            };

        public static IEnumerable<string> EnvironmentVars(string password) =>
            new[] { $"MYSQL_PWD={password}" };
    }

    public static class PostgreSql
    {
        public static IEnumerable<string> CreateBackup(string instanceName, string date) =>
            new[]
            {
                "pg_dump",
                "-U",
                DbEngineConfig.DefaultUser.PostgreSQL,
                DbEngineConfig.DefaultDatabase.dbName,
                ">",
                ContainerFilePath.PostgreSQL(instanceName, date),
            };

        public static IEnumerable<string> Restore(string filePath) =>
            new[]
            {
                "psql",
                "-U",
                DbEngineConfig.DefaultUser.PostgreSQL,
                DbEngineConfig.DefaultDatabase.dbName,
                "<",
                filePath,
            };

        public static IEnumerable<string> EnvironmentVars(string password) =>
            new[] { $"PGPASSWORD={password}" };
    }

    public static class MsSql
    {
        public static IEnumerable<string> CreateBackup(string instanceName, string date) =>
            new[]
            {
                "sqlcmd",
                "-Q",
                $"BACKUP DATABASE {DbEngineConfig.DefaultDatabase.dbName} TO DISK = '{ContainerFilePath.MSSQL(instanceName, date)}'",
            };

        public static IEnumerable<string> Restore(string filePath) =>
            new[]
            {
                "sqlcmd",
                "-Q",
                $"RESTORE DATABASE {DbEngineConfig.DefaultDatabase.dbName} FROM DISK = '{filePath}'",
            };

        public static IEnumerable<string> EnvironmentVars(string password) =>
            new[] { "ACCEPT_EULA=Y", $"SA_PASSWORD={password}" };
    }

    public static class MongoDb
    {
        public static IEnumerable<string> CreateBackup(string instanceName, string date) =>
            new[]
            {
                "mongodump",
                $"--uri=mongodb://{DbEngineConfig.DefaultUser.MongoDB}:<password>@localhost:27017/{DbEngineConfig.DefaultDatabase.dbName}",
                $"--out={ContainerFilePath.MongoDB(instanceName, date)}",
            };

        public static IEnumerable<string> Restore(string filePath) =>
            new[]
            {
                "mongorestore",
                $"--uri=mongodb://{DbEngineConfig.DefaultUser.MongoDB}:<password>@localhost:27017/{DbEngineConfig.DefaultDatabase.dbName}",
                $"--drop {filePath}",
            };

        public static IEnumerable<string> EnvironmentVars(string password) =>
            new[] { $"MONGO_INITDB_ROOT_PASSWORD={password}" };
    }
}

