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
