namespace WebApplication1.Models.DTOs.Responses;

public class RegisterResult
{
    public string OpenId { get; set; } = string.Empty;
    public string NickName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int UserId { get; set; }
}
