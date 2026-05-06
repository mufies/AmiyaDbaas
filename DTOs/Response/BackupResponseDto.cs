using System;

namespace AmiyaDbaasManager.DTOs.Response;

public class BackupResponseDto
{
    public string BackupPath { get; set; } = null!;
    public string FileName { get; set; } = null!;
}
