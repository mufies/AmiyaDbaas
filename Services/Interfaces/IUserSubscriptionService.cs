using AmiyaDbaasManager.Models;

namespace AmiyaDbaasManager.Services.Interfaces;

public interface IUserSubscriptionService
{
    /// <summary>Lấy subscription active hiện tại của user.</summary>
    Task<UserSubscription?> GetActiveSubscription(Guid userId);

    /// <summary>
    /// Assign plan mới cho user: deactivate subscription cũ (nếu có),
    /// tạo subscription mới với snapshot limit từ plan.
    /// </summary>
    Task<UserSubscription> AssignPlan(Guid userId, Guid planId);

    /// <summary>
    /// Kiểm tra user còn quota tạo instance không.
    /// Throw InvalidOperationException nếu đã đạt giới hạn hoặc chưa có subscription.
    /// </summary>
    Task ValidatePlanLimit(Guid userId);
    Task checkUserPlan();
}
