using Microsoft.EntityFrameworkCore;

namespace AmiyaDbaasManager.Models;

[Index(nameof(AllocatedPort), IsUnique = true)]
public class DbInstance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string InstanceName { get; set; } = string.Empty;
    public string Engine { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string Description { get; set; } = string.Empty;
    public int CpuCores { get; set; }
    public int RamMb { get; set; }
    public int StorageGb { get; set; }
    public int AllocatedPort { get; set; }
    public string Host { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }



    public Guid UserId { get; set; }
    public User? User { get; set; }
}
