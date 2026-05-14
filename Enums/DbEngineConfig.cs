namespace AmiyaDbaasManager.Enums;

/// <summary>
/// Tập hợp tất cả config cứng liên quan tới từng DB engine:
/// image name, tag, container port, env vars, connection string template.
/// </summary>
public static class DbEngineConfig
{
    // ─── Image names ────────────────────────────────────────────────────────────
    public static class ImageName
    {
        public const string MySQL = "mysql";
        public const string PostgreSQL = "postgres";
        public const string MSSQL = "mcr.microsoft.com/mssql/server";
        public const string MongoDB = "mongo";
    }

    // ─── Image tags (version) ───────────────────────────────────────────────────
    public static class ImageTag
    {
        public const string MySQL = "8.0";
        public const string PostgreSQL = "15";
        public const string MSSQL = "2022-latest";
        public const string MongoDB = "latest";
    }

    // ─── Internal container ports ────────────────────────────────────────────────
    public static class ContainerPort
    {
        public const string MySQL = "3306/tcp";
        public const string PostgreSQL = "5432/tcp";
        public const string MSSQL = "1433/tcp";
        public const string MongoDB = "27017/tcp";
    }

    // ─── Default DB users ────────────────────────────────────────────────────────
    public static class DefaultUser
    {
        public const string MySQL = "admin";
        public const string PostgreSQL = "admin";
        public const string MSSQL = "sa";
        public const string MongoDB = "admin";
    }

    // ─── Default DB name (per engine) ───────────────────────────────────────────
    public static class DefaultDatabase
    {
        public const string MySQL = "defaultDb";
        public const string PostgreSQL = "admin"; // PostgreSQL tạo DB trùng tên POSTGRES_USER
        public const string MSSQL = "defaultDb";
        public const string MongoDB = "defaultDb";

        public static string Get(DbEngine engine) =>
            engine switch
            {
                DbEngine.MySQL => MySQL,
                DbEngine.PostgreSQL => PostgreSQL,
                DbEngine.MSSQL => MSSQL,
                DbEngine.MongoDB => MongoDB,
                _ => throw new ArgumentOutOfRangeException(nameof(engine)),
            };
    }

    // ─── Helper methods ──────────────────────────────────────────────────────────

    /// <summary>Parse chuỗi engine từ request sang enum, throw nếu không hợp lệ.</summary>
    public static DbEngine ParseEngine(string engine) =>
        engine.ToLower() switch
        {
            "mysql" => DbEngine.MySQL,
            "postgresql" => DbEngine.PostgreSQL,
            "postgres" => DbEngine.PostgreSQL,
            "mssql" => DbEngine.MSSQL,
            "sqlserver" => DbEngine.MSSQL,
            "mongodb" => DbEngine.MongoDB,
            "mongo" => DbEngine.MongoDB,
            _ => throw new ArgumentException($"Unsupported engine: {engine}"),
        };

    public static string GetImageName(DbEngine engine) =>
        engine switch
        {
            DbEngine.MySQL => ImageName.MySQL,
            DbEngine.PostgreSQL => ImageName.PostgreSQL,
            DbEngine.MSSQL => ImageName.MSSQL,
            DbEngine.MongoDB => ImageName.MongoDB,
            _ => throw new ArgumentOutOfRangeException(nameof(engine)),
        };

    public static string GetImageTag(DbEngine engine) =>
        engine switch
        {
            DbEngine.MySQL => ImageTag.MySQL,
            DbEngine.PostgreSQL => ImageTag.PostgreSQL,
            DbEngine.MSSQL => ImageTag.MSSQL,
            DbEngine.MongoDB => ImageTag.MongoDB,
            _ => throw new ArgumentOutOfRangeException(nameof(engine)),
        };

    public static string GetContainerPort(DbEngine engine) =>
        engine switch
        {
            DbEngine.MySQL => ContainerPort.MySQL,
            DbEngine.PostgreSQL => ContainerPort.PostgreSQL,
            DbEngine.MSSQL => ContainerPort.MSSQL,
            DbEngine.MongoDB => ContainerPort.MongoDB,
            _ => throw new ArgumentOutOfRangeException(nameof(engine)),
        };

    /// <summary>
    /// Tên entrypoint Traefik tương ứng với từng engine (khai báo trong docker-compose).
    /// </summary>
    public static string GetTraefikEntrypointName(DbEngine engine) =>
        engine switch
        {
            DbEngine.MySQL    => "mysql",
            DbEngine.PostgreSQL => "postgres",
            DbEngine.MSSQL    => "mssql",
            DbEngine.MongoDB  => "mongodb",
            _ => throw new ArgumentOutOfRangeException(nameof(engine)),
        };

    /// <summary>
    /// Port public của Traefik entrypoint tương ứng với từng engine.
    /// </summary>
    public static int GetTraefikEntrypointPort(DbEngine engine) =>
        engine switch
        {
            DbEngine.MySQL      => 3306,
            DbEngine.PostgreSQL => 5432,
            DbEngine.MSSQL      => 1433,
            DbEngine.MongoDB    => 27017,
            _ => throw new ArgumentOutOfRangeException(nameof(engine)),
        };

    /// <summary>Trả về danh sách env vars cần inject vào container.</summary>
    public static List<string> GetEnvVars(DbEngine engine, string password) =>
        engine switch
        {
            DbEngine.MySQL => new List<string>
            {
                $"MYSQL_ROOT_PASSWORD={password}",
                $"MYSQL_USER={DefaultUser.MySQL}",
                $"MYSQL_PASSWORD={password}",
                $"MYSQL_DATABASE={DefaultDatabase.MySQL}",
            },
            DbEngine.PostgreSQL => new List<string>
            {
                $"POSTGRES_PASSWORD={password}",
                $"POSTGRES_USER={DefaultUser.PostgreSQL}",
                $"POSTGRES_DB={DefaultDatabase.PostgreSQL}",
            },
            DbEngine.MSSQL => new List<string> { $"SA_PASSWORD={password}", $"ACCEPT_EULA=Y" },
            DbEngine.MongoDB => new List<string>
            {
                $"MONGO_INITDB_ROOT_USERNAME={DefaultUser.MongoDB}",
                $"MONGO_INITDB_ROOT_PASSWORD={password}",
                $"MONGO_INITDB_DATABASE={DefaultDatabase.MongoDB}",
            },
            _ => throw new ArgumentOutOfRangeException(nameof(engine)),
        };

    /// <summary>Tạo connection string theo engine, host và port được cấp phát.</summary>
    public static string BuildConnectionString(
        DbEngine engine,
        string host,
        int port,
        string password
    ) =>
        engine switch
        {
            DbEngine.MySQL =>
                $"Server={host};Port={port};User Id={DefaultUser.MySQL};Password={password};Database={DefaultDatabase.MySQL}",
            DbEngine.PostgreSQL =>
                $"Host={host};Port={port};Username={DefaultUser.PostgreSQL};Password={password};Database={DefaultDatabase.PostgreSQL}",
            DbEngine.MSSQL =>
                $"Server={host},{port};User Id={DefaultUser.MSSQL};Password={password};Database={DefaultDatabase.MSSQL};",
            DbEngine.MongoDB =>
                $"mongodb://{DefaultUser.MongoDB}:{password}@{host}:{port}/{DefaultDatabase.MongoDB}",
            _ => throw new ArgumentOutOfRangeException(nameof(engine)),
        };
}
