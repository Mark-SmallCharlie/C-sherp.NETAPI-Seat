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
