using AmiyaDbaasManager.Models;

namespace AmiyaDbaasManager.Repositories.Interfaces;

public interface IPlanRepository
{
    Task<Plan> Create(Plan plan);
    Task<List<Plan>> GetAll();
    Task<Plan?> GetById(Guid planId);
    Task<Plan> Update(Plan plan);
}
