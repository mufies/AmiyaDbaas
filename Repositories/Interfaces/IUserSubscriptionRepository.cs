using AmiyaDbaasManager.Models;

namespace AmiyaDbaasManager.Repositories.Interfaces;

public interface IUserSubscriptionRepository
{
    Task<UserSubscription?> GetActiveByUserId(Guid userId);
    Task<UserSubscription> Create(UserSubscription subscription);
    Task<UserSubscription> Update(UserSubscription subscription);
}
