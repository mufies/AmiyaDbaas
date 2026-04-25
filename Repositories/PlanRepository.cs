using AmiyaDbaasManager.Data;
using AmiyaDbaasManager.Models;
using AmiyaDbaasManager.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AmiyaDbaasManager.Repositories;

public class PlanRepository : IPlanRepository
{
    private readonly AppDbContext _context;

    public PlanRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Plan> Create(Plan plan)
    {
        _context.Plans.Add(plan);
        await _context.SaveChangesAsync();
        return plan;
    }

    public async Task<List<Plan>> GetAll()
    {
        return await _context.Plans
            .Where(p => p.IsActive)
            .OrderBy(p => p.PriceMonthly)
            .ToListAsync();
    }

    public async Task<Plan?> GetById(Guid planId)
    {
        return await _context.Plans.FindAsync(planId);
    }

    public async Task<Plan> Update(Plan plan)
    {
        plan.CreatedAt = plan.CreatedAt; // preserve original
        _context.Plans.Update(plan);
        await _context.SaveChangesAsync();
        return plan;
    }
}
