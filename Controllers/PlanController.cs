using AmiyaDbaasManager.DTOs.Response;
using AmiyaDbaasManager.Models;
using AmiyaDbaasManager.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AmiyaDbaasManager.Controllers;

[ApiController]
[Route("api/plans")]
public class PlanController : ControllerBase
{
    private readonly IPlanService _planService;

    public PlanController(IPlanService planService)
    {
        _planService = planService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<Plan>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllPlans()
    {
        var plans = await _planService.GetAllPlans();
        return Ok(ApiResponse<List<Plan>>.Ok(plans, "Success"));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Plan>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreatePlan([FromBody] Plan plan)
    {
        // TODO: Sau này cần thêm Authorization để chỉ Admin mới được tạo Plan
        var created = await _planService.CreatePlan(plan);
        return Ok(ApiResponse<Plan>.Ok(created, "Tạo gói thành công"));
    }
}
