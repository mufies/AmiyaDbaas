using System.Security.Claims;
using AmiyaDbaasManager.DTOs;
using AmiyaDbaasManager.DTOs.Response;
using AmiyaDbaasManager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmiyaDbaasManager.Controllers;

[ApiController]
[Route("api/backup")]
[Authorize]
public class BackupController : ControllerBase
{
    private readonly IBackupService _backupService;
    private readonly IMinioService _minioService;

    public BackupController(IBackupService backupService, IMinioService minioService)
    {
        _backupService = backupService;
        _minioService = minioService;
    }

    [HttpPost("create")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateBackup(
        [FromBody] CreateBackupRequest request,
        CancellationToken ct
    )
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));

        try
        {
            await _backupService.CreateBackupForInstanceAsync(request, Guid.Parse(userId), ct);
            return Ok(ApiResponse<string>.Ok("Backup thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail($"Lỗi hệ thống: {ex.Message}"));
        }
    }

    [HttpPost("restore")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RestoreBackup(
        [FromBody] RestoreBackupRequest request,
        CancellationToken ct
    )
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));

        try
        {
            await _backupService.RestoreBackupForInstanceAsync(
                request.DbInstanceId,
                request.BackupObjectPath,
                Guid.Parse(userId),
                ct
            );
            return Ok(ApiResponse<string>.Ok("Restore thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail($"Lỗi hệ thống: {ex.Message}"));
        }
    }

    [HttpGet("list")]
    [ProducesResponseType(typeof(ApiResponse<List<BackupResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListBackups(
        [FromQuery] Guid? instanceId,
        CancellationToken ct
    )
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));

        try
        {
            var backups = await _minioService.ListBackupsAsync(Guid.Parse(userId), instanceId, ct);
            return Ok(ApiResponse<List<BackupResponseDto>>.Ok(backups));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail($"Lỗi hệ thống: {ex.Message}"));
        }
    }
}
