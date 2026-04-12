namespace WebApplication1.Models.DTOs.Requests;

/// <summary>
/// 小程序：账号密码注册时填写 Password；仅微信资料注册时可省略，走待审核流程。
/// </summary>
public class RegisterRequest
{
    public string OpenId { get; set; } = string.Empty;
    public string NickName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    /// <summary>可选。若提供则写入 PasswordHash，并默认将角色设为 User（可直接密码登录）。</summary>
    public string? Password { get; set; }
}
