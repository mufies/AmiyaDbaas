namespace AmiyaDbaasManager.DTOs;

public class RestoreBackupRequest
{
    public Guid DbInstanceId { get; set; }
    public string BackupObjectPath { get; set; } = string.Empty;
}
