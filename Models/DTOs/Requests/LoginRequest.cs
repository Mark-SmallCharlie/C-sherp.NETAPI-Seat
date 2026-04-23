// 这是一个登录请求的DTO类，包含用户名和密码属性，用于接收登录请求的数据。
namespace WebApplication1.Models.DTOs.Requests
{
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
