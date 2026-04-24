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
        public const string MySQL      = "mysql";
        public const string PostgreSQL = "postgres";
    }

    // ─── Image tags (version) ───────────────────────────────────────────────────
    public static class ImageTag
    {
        public const string MySQL      = "8.0";
        public const string PostgreSQL = "15";
    }

    // ─── Internal container ports ────────────────────────────────────────────────
    public static class ContainerPort
    {
        public const string MySQL      = "3306/tcp";
        public const string PostgreSQL = "5432/tcp";
    }

    // ─── Default DB users ────────────────────────────────────────────────────────
    public static class DefaultUser
    {
        public const string MySQL      = "admin";
        public const string PostgreSQL = "admin";
    }

    // ─── Default DB name (MySQL only) ────────────────────────────────────────────
    public static class DefaultDatabase
    {
        public const string MySQL = "db";
    }

    // ─── Helper methods ──────────────────────────────────────────────────────────

    /// <summary>Parse chuỗi engine từ request sang enum, throw nếu không hợp lệ.</summary>
    public static DbEngine ParseEngine(string engine) =>
        engine.ToLower() switch
        {
            "mysql"      => DbEngine.MySQL,
            "postgresql" => DbEngine.PostgreSQL,
            "postgres"   => DbEngine.PostgreSQL,
            _ => throw new ArgumentException($"Unsupported engine: {engine}")
        };

    public static string GetImageName(DbEngine engine) => engine switch
    {
        DbEngine.MySQL      => ImageName.MySQL,
        DbEngine.PostgreSQL => ImageName.PostgreSQL,
        _ => throw new ArgumentOutOfRangeException(nameof(engine))
    };

    public static string GetImageTag(DbEngine engine) => engine switch
    {
        DbEngine.MySQL      => ImageTag.MySQL,
        DbEngine.PostgreSQL => ImageTag.PostgreSQL,
        _ => throw new ArgumentOutOfRangeException(nameof(engine))
    };

    public static string GetContainerPort(DbEngine engine) => engine switch
    {
        DbEngine.MySQL      => ContainerPort.MySQL,
        DbEngine.PostgreSQL => ContainerPort.PostgreSQL,
        _ => throw new ArgumentOutOfRangeException(nameof(engine))
    };

    /// <summary>Trả về danh sách env vars cần inject vào container.</summary>
    public static List<string> GetEnvVars(DbEngine engine, string password) => engine switch
    {
        DbEngine.MySQL => new List<string>
        {
            $"MYSQL_ROOT_PASSWORD={password}",
            $"MYSQL_USER={DefaultUser.MySQL}",
            $"MYSQL_PASSWORD={password}",
            $"MYSQL_DATABASE={DefaultDatabase.MySQL}"
        },
        DbEngine.PostgreSQL => new List<string>
        {
            $"POSTGRES_PASSWORD={password}",
            $"POSTGRES_USER={DefaultUser.PostgreSQL}"
        },
        _ => throw new ArgumentOutOfRangeException(nameof(engine))
    };

    /// <summary>Tạo connection string theo engine, host và port được cấp phát.</summary>
    public static string BuildConnectionString(DbEngine engine, string host, int port, string password) =>
        engine switch
        {
            DbEngine.MySQL =>
                $"Server={host};Port={port};User Id={DefaultUser.MySQL};Password={password};Database={DefaultDatabase.MySQL}",
            DbEngine.PostgreSQL =>
                $"Host={host};Port={port};Username={DefaultUser.PostgreSQL};Password={password}",
            _ => throw new ArgumentOutOfRangeException(nameof(engine))
        };
}
