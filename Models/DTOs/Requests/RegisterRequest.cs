namespace WebApplication1.Models.DTOs.Responses
{
    public class RegisterResult
    {
        public string OpenId { get; set; } = string.Empty;
        public string NickName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public int UserId { get; set; } 
        // 可根据实际需求添加更多字段
    }
    public class RegisterRequest
    {
        public string OpenId { get; set; }
        public string NickName { get; set; }
        public string? AvatarUrl { get; set; }
    }
}