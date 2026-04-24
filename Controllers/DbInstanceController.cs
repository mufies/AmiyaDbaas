using System.Security.Claims;
using AmiyaDbaasManager.DTOs.Response;
using AmiyaDbaasManager.DTOs.Response.DbInstance;
using AmiyaDbaasManager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmiyaDbaasManager.Controllers;

[ApiController]
[Route("api/dbinstances")]
[Authorize]
public class DbInstanceController : ControllerBase
{
    private readonly IDbInstanceService _dbInstanceService;

    public DbInstanceController(IDbInstanceService dbInstanceService)
    {
        _dbInstanceService = dbInstanceService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<DbInstanceResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var instances = await _dbInstanceService.GetAll();
        return Ok(ApiResponse<List<DbInstanceResponseDto>>.Ok(instances, "Lấy danh sách thành công"));
    }

    [HttpGet("my-instances")]
    [ProducesResponseType(typeof(ApiResponse<List<DbInstanceResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyInstances()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));

        var instances = await _dbInstanceService.GetByUser(userId);
        return Ok(ApiResponse<List<DbInstanceResponseDto>>.Ok(instances, "Lấy danh sách instance của bạn thành công"));
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    [HttpPatch("{instanceId}/status")]
    [ProducesResponseType(typeof(ApiResponse<DbInstanceResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid instanceId, [FromBody] UpdateStatusRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(ApiResponse<object>.Fail("Trạng thái (Status) không được để trống"));

        var result = await _dbInstanceService.UpdateStatus(instanceId, request.Status);
        
        if (result == null)
            return NotFound(ApiResponse<object>.Fail($"Không tìm thấy instance với ID: {instanceId}"));
            
        return Ok(ApiResponse<DbInstanceResponseDto>.Ok(result, "Cập nhật trạng thái thành công"));
    }
}
