// 这是一个用于微信登录请求的DTO类，包含了微信临时登录凭证、OpenId、昵称和头像URL等属性。它可以用于接收前端发送的微信登录请求数据，并在后端进行处理。
namespace WebApplication1.Models.DTOs.Requests
{
    public class WechatLoginRequest
    {
        public string Code { get; set; } = string.Empty; // 微信临时登录凭证
        public string? OpenId { get; set; }             // 或直接使用OpenId（演示用）
        public string NickName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }
}
