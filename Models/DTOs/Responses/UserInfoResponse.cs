/**
 * 这是一个用于封装用户信息的响应DTO类，包含了用户的基本信息，如ID、昵称、头像URL、角色和显示名称等属性。
 * 这个类可以用于在API响应中返回用户信息，特别是在管理员界面中显示用户的详细信息。
 */
namespace WebApplication1.Models.DTOs.Responses
{
    public class UserInfoResponse
    {
        public int Id { get; set; }
        public string NickName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Role { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty; // 用于管理员
    }
}
