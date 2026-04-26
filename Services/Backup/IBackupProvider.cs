namespace AmiyaDbaasManager.Services.Backup
{
    public interface IBackupProvider
    {
        IEnumerable<string> CreateBackupCmd(string instanceName, string date);

        IEnumerable<string> RestoreDataCmd(string filePath);

        IEnumerable<string> GetEnvVars(string password);
    }
}
