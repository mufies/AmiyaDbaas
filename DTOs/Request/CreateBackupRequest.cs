namespace AmiyaDbaasManager.DTOs;

public class CreateBackupRequest
{
    public string? ContainerId { get; set; }
    public string? Password { get; set; }
    public string? InstanceType { get; set; }
}

