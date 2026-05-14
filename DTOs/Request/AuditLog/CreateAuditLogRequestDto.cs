namespace AmiyaDbaasManager.DTOs.Request.AuditLog;

public class CreateAuditLogRequestDto
{
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}
