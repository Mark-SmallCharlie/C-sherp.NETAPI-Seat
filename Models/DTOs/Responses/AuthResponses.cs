/***
 * 这是一个用于管理员用户响应的DTO类，包含用户的ID、用户名、显示名称、是否激活以及创建时间等属性。
 ***/
using WebApplication1.Models.DTOs.Requests;
using WebApplication1.Models.DTOs.Responses;

namespace WebApplication1.Models.DTOs.Responses;


public class AdminUserResponse
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
