using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models.Entities;

/// <summary>
/// 候补排队实体 — 座位被约满时用户可加入候补队列，
/// 有人取消时按排队顺序自动通知。
/// </summary>
public class WaitlistEntry
{
    [Key]
    public int Id { get; set; }

    [Required, ForeignKey("User")]
    public int UserId { get; set; }

    [Required]
    public int SeatNumber { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    [Required]
    public WaitlistStatus Status { get; set; } = WaitlistStatus.Waiting;

    /// <summary>排队位置（同座位+时段内排序）</summary>
    public int QueuePosition { get; set; }

    /// <summary>通知时间</summary>
    public DateTime? NotifiedAt { get; set; }

    /// <summary>确认截止时间（超时自动顺延）</summary>
    public DateTime? ConfirmDeadline { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 导航属性
    public virtual User? User { get; set; }
}

public enum WaitlistStatus
{
    Waiting,    // 排队中
    Notified,   // 已通知（等待确认）
    Confirmed,  // 已确认（转预约）
    Expired,    // 确认超时
    Cancelled   // 用户主动取消
}
