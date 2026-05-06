using AmiyaDbaasManager.Enums;

public static class BackupCommands
{
    public static class ContainerFilePath
    {
        public static string MySQL(string instanceName, string date) =>
            $"/tempfiles/mysql/{instanceName}_{date}/backup_{date}.sql";

        public static string PostgreSQL(string instanceName, string date) =>
            $"/tempfiles/postgresql/{instanceName}_{date}/backup_{date}.sql";

        public static string MSSQL(string instanceName, string date) =>
            $"/tempfiles/mssql/{instanceName}_{date}/backup_{date}.bak";

        public static string MongoDB(string instanceName, string date) =>
            $"/tempfiles/mongodb/{instanceName}_{date}";

        public static string Get(DbEngine engine, string instanceName, string date) =>
            engine switch
            {
                DbEngine.MySQL => MySQL(instanceName, date),
                DbEngine.PostgreSQL => PostgreSQL(instanceName, date),
                DbEngine.MSSQL => MSSQL(instanceName, date),
                DbEngine.MongoDB => MongoDB(instanceName, date),
                _ => throw new ArgumentOutOfRangeException(nameof(engine)),
            };
    }

    public static class MySql
    {
        public static IEnumerable<string> CreateBackup(string instanceName, string date) =>
            new[]
            {
                "/bin/sh",
                "-c",
                $"set -e && mkdir -p $(dirname {ContainerFilePath.MySQL(instanceName, date)}) && "
                    + $"mysqldump -u{DbEngineConfig.DefaultUser.MySQL} --single-transaction --routines --triggers "
                    + $"{DbEngineConfig.DefaultDatabase.MySQL} > {ContainerFilePath.MySQL(instanceName, date)}",
            };

        public static IEnumerable<string> Restore(string filePath) =>
            new[]
            {
                "/bin/sh",
                "-c",
                $"mysql -u{DbEngineConfig.DefaultUser.MySQL} {DbEngineConfig.DefaultDatabase.MySQL} < {filePath}",
            };

        public static IEnumerable<string> EnvironmentVars(string password) =>
            new[] { $"MYSQL_PWD={password}" };
    }

    public static class PostgreSql
    {
        public static IEnumerable<string> CreateBackup(string instanceName, string date) =>
            new[]
            {
                "/bin/sh",
                "-c",
                $"set -e && mkdir -p $(dirname {ContainerFilePath.PostgreSQL(instanceName, date)}) && "
                    + $"pg_dump -U {DbEngineConfig.DefaultUser.PostgreSQL} "
                    + $"{DbEngineConfig.DefaultDatabase.PostgreSQL} > {ContainerFilePath.PostgreSQL(instanceName, date)}",
            };

        public static IEnumerable<string> Restore(string filePath) =>
            new[]
            {
                "/bin/sh",
                "-c",
                $"psql -U {DbEngineConfig.DefaultUser.PostgreSQL} {DbEngineConfig.DefaultDatabase.PostgreSQL} < {filePath}",
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
                "-S",
                "localhost",
                "-U",
                DbEngineConfig.DefaultUser.MSSQL,
                "-Q",
                $"BACKUP DATABASE {DbEngineConfig.DefaultDatabase.MSSQL} TO DISK = '{ContainerFilePath.MSSQL(instanceName, date)}'",
            };

        public static IEnumerable<string> Restore(string filePath) =>
            new[]
            {
                "sqlcmd",
                "-S",
                "localhost",
                "-U",
                DbEngineConfig.DefaultUser.MSSQL,
                "-Q",
                $"RESTORE DATABASE {DbEngineConfig.DefaultDatabase.MSSQL} FROM DISK = '{filePath}' WITH REPLACE",
            };

        public static IEnumerable<string> EnvironmentVars(string password) =>
            new[] { "ACCEPT_EULA=Y", $"SA_PASSWORD={password}" };
    }

    public static class MongoDb
    {
        public static IEnumerable<string> CreateBackup(
            string instanceName,
            string date,
            string password
        ) =>
            new[]
            {
                "mongodump",
                $"--uri=mongodb://{DbEngineConfig.DefaultUser.MongoDB}:{password}@127.0.0.1:27017/{DbEngineConfig.DefaultDatabase.MongoDB}",
                $"--out={ContainerFilePath.MongoDB(instanceName, date)}",
            };

        public static IEnumerable<string> Restore(string filePath, string password) =>
            new[]
            {
                "mongorestore",
                $"--uri=mongodb://{DbEngineConfig.DefaultUser.MongoDB}:{password}@127.0.0.1:27017/{DbEngineConfig.DefaultDatabase.MongoDB}",
                "--drop",
                filePath,
            };

        public static IEnumerable<string> EnvironmentVars(string password) =>
            new[] { $"MONGO_INITDB_ROOT_PASSWORD={password}" };
    }

    // ─── Helper dùng enum ────────────────────────────────────────────────────────

    public static IEnumerable<string> GetBackupCmd(
        DbEngine engine,
        string instanceName,
        string date,
        string password
    ) =>
        engine switch
        {
            DbEngine.MySQL => MySql.CreateBackup(instanceName, date),
            DbEngine.PostgreSQL => PostgreSql.CreateBackup(instanceName, date),
            DbEngine.MSSQL => MsSql.CreateBackup(instanceName, date),
            DbEngine.MongoDB => MongoDb.CreateBackup(instanceName, date, password),
            _ => throw new ArgumentOutOfRangeException(nameof(engine)),
        };

    public static IEnumerable<string> GetRestoreCmd(
        DbEngine engine,
        string filePath,
        string password
    ) =>
        engine switch
        {
            DbEngine.MySQL => MySql.Restore(filePath),
            DbEngine.PostgreSQL => PostgreSql.Restore(filePath),
            DbEngine.MSSQL => MsSql.Restore(filePath),
            DbEngine.MongoDB => MongoDb.Restore(filePath, password),
            _ => throw new ArgumentOutOfRangeException(nameof(engine)),
        };

    public static IEnumerable<string> GetEnvVars(DbEngine engine, string password) =>
        engine switch
        {
            DbEngine.MySQL => MySql.EnvironmentVars(password),
            DbEngine.PostgreSQL => PostgreSql.EnvironmentVars(password),
            DbEngine.MSSQL => MsSql.EnvironmentVars(password),
            DbEngine.MongoDB => MongoDb.EnvironmentVars(password),
            _ => throw new ArgumentOutOfRangeException(nameof(engine)),
        };
}

