using System.ComponentModel.DataAnnotations;
namespace WebApplication1.Models.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string OpenId { get; set; } = string.Empty; // 微信用户唯一标识

        [Required, MaxLength(50)]
        public string NickName { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; } = UserRole.Pending; // 用户角色状态

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(1000)]
        public string? AvatarUrl { get; set; } // 用户头像URL

        /// <summary>账号密码注册时写入 SHA256 十六进制哈希；微信用户可为空。</summary>
        [MaxLength(128)]
        public string? PasswordHash { get; set; }

        // 导航属性 - 该用户的所有预约
        public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }

    public enum UserRole
    {
        Pending,  // 待审核
        User,     // 普通用户
        Admin,    // 管理员 (通常不走微信流程，但保留枚举)
        Rejected  // 已拒绝
    }
}
