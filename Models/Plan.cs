namespace AmiyaDbaasManager.Models;

public class Plan
{
    public Guid PlanId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // ── Resource limits ──────────────────────────────────────
    public int MaxInstances { get; set; }
    public int MaxCpuCoresPerInstance { get; set; }
    public int MaxRamMbPerInstance { get; set; }
    public int MaxStorageGbPerInstance { get; set; }

    // ── Pricing ──────────────────────────────────────────────
    public decimal PriceMonthly { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
}
