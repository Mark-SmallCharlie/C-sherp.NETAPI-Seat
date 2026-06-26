using System.ComponentModel.DataAnnotations;
/**
 * AdminUser实体类定义了管理员用户的数据结构，包含了Id、Username、PasswordHash、DisplayName、IsActive和CreatedAt等属性。
 * 这些属性分别表示管理员用户的唯一标识、登录用户名、哈希后的密码、显示名称、是否激活以及创建时间。
 * 通过这个类，开发者可以方便地在数据库中存储和管理管理员用户的信息。
 */
namespace WebApplication1.Models.Entities
{
    public class AdminUser
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty; // 登录用户名

        [Required, MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty; // BCrypt 哈希后的密码 (~60 chars)

        [Required, MaxLength(50)]
        public string DisplayName { get; set; } = string.Empty; // 显示名称

        [Required]
        public bool IsActive { get; set; } = true; // 是否激活

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
