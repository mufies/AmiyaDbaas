using AmiyaDbaasManager.Models;
using AmiyaDbaasManager.Repositories.Interfaces;
using AmiyaDbaasManager.Services.Interfaces;

namespace AmiyaDbaasManager.Services;

public class UserSubscriptionService : IUserSubscriptionService
{
    private readonly IUserSubscriptionRepository _subscriptionRepo;
    private readonly IPlanRepository _planRepo;
    private readonly IDbInstanceRepo _instanceRepo;

    public UserSubscriptionService(
        IUserSubscriptionRepository subscriptionRepo,
        IPlanRepository planRepo,
        IDbInstanceRepo instanceRepo)
    {
        _subscriptionRepo = subscriptionRepo;
        _planRepo = planRepo;
        _instanceRepo = instanceRepo;
    }

    public async Task<UserSubscription?> GetActiveSubscription(Guid userId)
    {
        return await _subscriptionRepo.GetActiveByUserId(userId);
    }

    public async Task<UserSubscription> AssignPlan(Guid userId, Guid planId)
    {
        var plan = await _planRepo.GetById(planId)
            ?? throw new KeyNotFoundException($"Plan {planId} không tồn tại.");

        // Deactivate subscription cũ (nếu có)
        var existing = await _subscriptionRepo.GetActiveByUserId(userId);
        if (existing is not null)
        {
            existing.IsActive = false;
            existing.EndDate = DateTime.UtcNow;
            await _subscriptionRepo.Update(existing);
        }

        // Tạo subscription mới – snapshot limit từ plan hiện tại
        var subscription = new UserSubscription
        {
            UserId = userId,
            PlanId = plan.PlanId,
            MaxInstances = plan.MaxInstances,
            MaxCpuCoresPerInstance = plan.MaxCpuCoresPerInstance,
            MaxRamMbPerInstance = plan.MaxRamMbPerInstance,
            MaxStorageGbPerInstance = plan.MaxStorageGbPerInstance,
            StartDate = DateTime.UtcNow,
            IsActive = true,
        };

        return await _subscriptionRepo.Create(subscription);
    }

    public async Task ValidatePlanLimit(Guid userId)
    {
        var subscription = await _subscriptionRepo.GetActiveByUserId(userId);

        if (subscription is null)
            throw new InvalidOperationException(
                "Bạn chưa có gói subscription. Vui lòng đăng ký một gói để tiếp tục.");

        // Kiểm tra thời hạn
        if (subscription.EndDate.HasValue && subscription.EndDate.Value < DateTime.UtcNow)
            throw new InvalidOperationException(
                "Gói subscription của bạn đã hết hạn. Vui lòng gia hạn để tiếp tục.");

        // Đếm số instance hiện tại của user
        var instances = await _instanceRepo.GetDbInstanceOfUser(userId.ToString());
        var currentCount = instances.Count;

        if (currentCount >= subscription.MaxInstances)
            throw new InvalidOperationException(
                $"Bạn đã đạt giới hạn {subscription.MaxInstances} instance của gói '{subscription.Plan?.Name ?? "hiện tại"}'. " +
                "Vui lòng nâng cấp gói để tạo thêm.");
    }
}
