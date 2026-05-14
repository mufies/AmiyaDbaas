namespace AmiyaDbaasManager.Models;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Action { get; set; } = string.Empty; // Create, Start, Stop, Delete, Backup, Restore
    public string EntityType { get; set; } = string.Empty; // DbInstance, Backup, etc.
    public string InstanceId { get; set; } = string.Empty; // e.g., DbInstance.Id
    public string Details { get; set; } = string.Empty; // JSON or text details
    public string IpAddress { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
