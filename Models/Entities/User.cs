using System.ComponentModel.DataAnnotations;

/**
 * User实体类定义了用户的数据结构，包含了Id、OpenId、NickName、Role、CreatedAt、AvatarUrl和PasswordHash等属性。
 * 这些属性分别表示用户的唯一标识、微信用户唯一标识、昵称、用户角色状态、创建时间、头像URL以及账号密码注册时的哈希值。
 * 通过这个类，开发者可以方便地在数据库中存储和管理用户的信息，并与预约信息进行关联。
 */
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
