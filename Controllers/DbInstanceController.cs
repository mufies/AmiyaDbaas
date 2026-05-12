using System.Security.Claims;
using AmiyaDbaasManager.DTOs.Request.DbInstance;
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
    private readonly IQueryRunService _queryRunService;

    public DbInstanceController(IDbInstanceService dbInstanceService, IQueryRunService queryRunService)
    {
        _dbInstanceService = dbInstanceService;
        _queryRunService = queryRunService;
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

    [HttpPost("execute-query")]
    [ProducesResponseType(typeof(ApiResponse<QueryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExecuteQuery([FromBody] CreateQueryRequestDto request)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));

        // Cập nhật UserId từ token để bảo mật, không tin tưởng body request
        request.UserId = userId;

        var result = await _queryRunService.RunQueryAsync(request);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<object>.Fail(result.ErrorMessage ?? "Thực thi truy vấn thất bại"));
        }

        return Ok(ApiResponse<QueryResponseDto>.Ok(result, "Thực thi truy vấn thành công"));
    }
}
