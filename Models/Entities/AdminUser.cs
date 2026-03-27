using System.ComponentModel.DataAnnotations;
namespace WebApplication1.Models.Entities
{
    public class AdminUser
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty; // 登录用户名

        [Required, MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty; // 哈希后的密码

        [Required, MaxLength(50)]
        public string DisplayName { get; set; } = string.Empty; // 显示名称

        [Required]
        public bool IsActive { get; set; } = true; // 是否激活

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
