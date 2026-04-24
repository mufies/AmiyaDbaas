using System.ComponentModel.DataAnnotations;

namespace AmiyaDbaasManager.DTOs.Request.DbInstance;

public class CreateDbInstanceRequestDto
{
    [Required(ErrorMessage = "Instance Name is required")]
    [RegularExpression(
        @"^[a-zA-Z0-9_-]+$",
        ErrorMessage = "Instance name can only contain alphanumeric characters, underscores, and hyphens"
    )]
    public string InstanceName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Engine is required")]
    public string Engine { get; set; } = string.Empty; // e.g., PostgreSQL, MySQL

    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "User ID is required")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Host is required")]
    public string Host { get; set; } = string.Empty;

    [Required(ErrorMessage = "CPU Cores are required")]
    public int CpuCores { get; set; }

    [Required(ErrorMessage = "RAM is required")]
    public int RamMb { get; set; }

    [Required(ErrorMessage = "Storage is required")]
    public int StorageGb { get; set; }
}
