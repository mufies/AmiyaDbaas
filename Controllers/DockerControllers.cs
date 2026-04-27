using System.Security.Claims;
using AmiyaDbaasManager.DTOs.Request.DbInstance;
using AmiyaDbaasManager.DTOs.Response;
using AmiyaDbaasManager.DTOs.Response.DbInstance;
using AmiyaDbaasManager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmiyaDbaasManager.Controllers;

[ApiController]
[Route("api/docker")]
[Authorize]
public class DockerControllers : ControllerBase
{
    private readonly IDockerService _dockerService;

    public DockerControllers(IDockerService dockerService)
    {
        _dockerService = dockerService;
    }

    [HttpPost("create")]
    [ProducesResponseType(typeof(ApiResponse<DbInstanceResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDockerDbImage(
        [FromBody] CreateDbInstanceRequestDto request
    )
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));
        request.UserId = Guid.Parse(userId);

        var result = await _dockerService.createDbImage(request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("start")]
    [ProducesResponseType(typeof(ApiResponse<DbInstanceResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartInstance([FromBody] InstanceActionRequestDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));

        var result = await _dockerService.StartInstanceAsync(request.InstanceId, userId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("stop")]
    [ProducesResponseType(typeof(ApiResponse<DbInstanceResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StopInstance([FromBody] InstanceActionRequestDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));

        var result = await _dockerService.StopInstanceAsync(request.InstanceId, userId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse<DbInstanceResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteInstance([FromBody] InstanceActionRequestDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));
        var result = await _dockerService.DeleteInstanceAsync(request.InstanceId, userId);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
