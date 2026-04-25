using System.Security.Claims;
using AmiyaDbaasManager.DTOs.Response;
using AmiyaDbaasManager.Models;
using AmiyaDbaasManager.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmiyaDbaasManager.Controllers;

[ApiController]
[Route("api/subscriptions")]
[Authorize]
public class SubscriptionController : ControllerBase
{
    private readonly IUserSubscriptionService _subscriptionService;

    public SubscriptionController(IUserSubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [HttpPost("subscribe/{planId}")]
    [ProducesResponseType(typeof(ApiResponse<UserSubscription>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Subscribe(Guid planId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));
        }

        try
        {
            // TODO: Tương lai xử lý logic thanh toán ở đây (kiểm tra số dư, gọi cổng thanh toán...)
            
            var subscription = await _subscriptionService.AssignPlan(userId, planId);
            return Ok(ApiResponse<UserSubscription>.Ok(subscription, "Đăng ký gói thành công."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail($"Lỗi khi đăng ký gói: {ex.Message}"));
        }
    }

    [HttpGet("my-subscription")]
    [ProducesResponseType(typeof(ApiResponse<UserSubscription>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySubscription()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));
        }

        var subscription = await _subscriptionService.GetActiveSubscription(userId);
        if (subscription == null)
        {
            return Ok(ApiResponse<object>.Ok(new { }, "Bạn chưa có gói đăng ký nào."));
        }

        return Ok(ApiResponse<UserSubscription>.Ok(subscription, "Success"));
    }
}
