namespace WebApplication1.Models.DTOs.Responses;
/***
 * 这是一个用于注册结果的DTO类，包含了用户的OpenId、昵称、头像URL、注册是否成功、
 * 消息以及用户ID等属性。它可以用于返回前端注册操作的结果信息。
 */
public class RegisterResult
{
    public string OpenId { get; set; } = string.Empty;
    public string NickName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int UserId { get; set; }
}
