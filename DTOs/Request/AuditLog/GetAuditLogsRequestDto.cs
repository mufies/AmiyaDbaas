namespace AmiyaDbaasManager.DTOs.Request.AuditLog;

public class GetAuditLogsRequestDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public string? InstanceId { get; set; }
    // Có thể lọc từ ngày đến ngày
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
