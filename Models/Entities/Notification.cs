using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models.Entities;

/// <summary>
/// 用户通知实体，记录系统发送给用户的通知消息。
/// 类型包括：预约开始提醒、超时预警、强制释放、封禁通知、系统消息等。
/// </summary>
public class Notification
{
    [Key]
    public int Id { get; set; }

    [Required, ForeignKey("User")]
    public int UserId { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string Content { get; set; } = string.Empty;

    [Required]
    public NotificationType Type { get; set; } = NotificationType.SystemMessage;

    public bool IsRead { get; set; } = false;

    /// <summary>关联的预约ID（可为空）</summary>
    public int? RelatedReservationId { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 导航属性
    public virtual User? User { get; set; }
}

public enum NotificationType
{
    ReservationStart,   // 预约即将开始
    TimeoutWarning,     // 暂离/超时预警
    ForceReleased,      // 预约被强制释放
    Suspended,          // 账号被冻结/封禁
    BanExpired,         // 封禁已解除
    SystemMessage,      // 系统消息
    WaitlistAvailable   // 候补可用通知
}
