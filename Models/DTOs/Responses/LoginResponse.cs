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
