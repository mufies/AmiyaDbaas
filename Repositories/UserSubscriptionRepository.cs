using AmiyaDbaasManager.Data;
using AmiyaDbaasManager.Models;
using AmiyaDbaasManager.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AmiyaDbaasManager.Repositories;

public class UserSubscriptionRepository : IUserSubscriptionRepository
{
    private readonly AppDbContext _context;

    public UserSubscriptionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserSubscription?> GetActiveByUserId(Guid userId)
    {
        return await _context.UserSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<UserSubscription> Create(UserSubscription subscription)
    {
        _context.UserSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();
        return subscription;
    }

    public async Task<UserSubscription> Update(UserSubscription subscription)
    {
        subscription.UpdatedAt = DateTime.UtcNow;
        _context.UserSubscriptions.Update(subscription);
        await _context.SaveChangesAsync();
        return subscription;
    }
}
