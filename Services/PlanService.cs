using AmiyaDbaasManager.Models;
using AmiyaDbaasManager.Repositories.Interfaces;
using AmiyaDbaasManager.Services.Interfaces;

namespace AmiyaDbaasManager.Services;

public class PlanService : IPlanService
{
    private readonly IPlanRepository _planRepository;

    public PlanService(IPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public async Task<Plan> CreatePlan(Plan plan)
    {
        return await _planRepository.Create(plan);
    }

    public async Task<List<Plan>> GetAllPlans()
    {
        return await _planRepository.GetAll();
    }

    public async Task<Plan?> UpdatePlan(Guid planId, Plan updatedData)
    {
        var existing = await _planRepository.GetById(planId);
        if (existing is null) return null;

        // Chỉ cập nhật các trường được phép thay đổi
        existing.Name = updatedData.Name;
        existing.Description = updatedData.Description;
        existing.MaxInstances = updatedData.MaxInstances;
        existing.MaxCpuCoresPerInstance = updatedData.MaxCpuCoresPerInstance;
        existing.MaxRamMbPerInstance = updatedData.MaxRamMbPerInstance;
        existing.MaxStorageGbPerInstance = updatedData.MaxStorageGbPerInstance;
        existing.PriceMonthly = updatedData.PriceMonthly;
        existing.IsActive = updatedData.IsActive;

        return await _planRepository.Update(existing);
    }
}
