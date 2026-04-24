namespace AmiyaDbaasManager.DTOs.Response.DbInstance;

public class DbInstanceResponseDto
{
    public Guid Id { get; set; }
    public string InstanceName { get; set; } = string.Empty;
    public string Engine { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CpuCores { get; set; }
    public int RamMb { get; set; }
    public int StorageGb { get; set; }
    public int AllocatedPort { get; set; }
    public string Host { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Guid UserId { get; set; }
}
