using AmiyaDbaasManager.Models;

namespace AmiyaDbaasManager.Services.Interfaces;

public interface IPlanService
{
    Task<Plan> CreatePlan(Plan plan);
    Task<List<Plan>> GetAllPlans();
    Task<Plan?> UpdatePlan(Guid planId, Plan updatedData);
}
