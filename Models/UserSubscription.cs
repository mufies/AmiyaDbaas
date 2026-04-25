namespace AmiyaDbaasManager.Models;

public class UserSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // ── FK ───────────────────────────────────────────────────
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }

    // ── Snapshot giới hạn tại thời điểm subscribe ───────────
    // Lưu riêng để tránh bị ảnh hưởng khi admin cập nhật plan
    public int MaxInstances { get; set; }
    public int MaxCpuCoresPerInstance { get; set; }
    public int MaxRamMbPerInstance { get; set; }
    public int MaxStorageGbPerInstance { get; set; }

    // ── Tracking thời hạn ────────────────────────────────────
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }   // null = không hết hạn
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public User? User { get; set; }
    public Plan? Plan { get; set; }
}
