using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models.Entities
{
    public class Reservation
    {
        [Key]
        public int Id { get; set; }

        [Required, ForeignKey("User")]
        public int UserId { get; set; } // 关联的用户ID

        [Required]
        public int SeatNumber { get; set; } // 座位编号

        [Required]
        public DateTime StartTime { get; set; } // 预约开始时间

        [Required]
        public DateTime EndTime { get; set; } // 预约结束时间

        [Required]
        public ReservationStatus Status { get; set; } = ReservationStatus.Active; // 预约状态

        public string? AdminNote { get; set; } // 管理员强制取消时的备注

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // 创建时间

        // 导航属性 - 预约所属的用户
        public virtual User User { get; set; } = new User();
    }
    public enum ReservationStatus
    {
        Active,         // 活跃/有效
        Completed,      // 已完成（时间已过）
        Cancelled,      // 用户自己取消
        ForceCancelled  // 管理员强制取消
    }
}
