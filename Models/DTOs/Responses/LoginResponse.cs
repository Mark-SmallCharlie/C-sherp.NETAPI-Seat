/***
 * 这是一个用于登录响应的DTO类，包含登录是否成功、令牌、用户信息、是否需要审批以及消息等属性。
 ***/
namespace WebApplication1.Models.DTOs.Responses
{
    public class LoginResponse
    {
        public bool Success { get; set; }

        public string Token { get; set; } = string.Empty;
        public UserInfoResponse? UserInfo { get; set; }
        public bool RequiresApproval { get; set; }
        public string? Message { get; set; }
    }
}
