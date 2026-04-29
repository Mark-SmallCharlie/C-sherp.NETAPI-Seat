using WebApplication1.Models.DTOs.Requests;
using WebApplication1.Models.DTOs.Responses;
/**
 * 这是一个用于定义认证服务接口的代码文件。
 * IAuthService接口包含了管理员登录、用户密码登录、微信登录以及生成JWT令牌的方法。
 * 这些方法分别用于处理不同类型的登录请求，并生成相应的登录响应或JWT令牌。
 * 通过实现这个接口，开发者可以在应用程序中提供多种认证方式，以满足不同用户的需求。
 */
namespace WebApplication1.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> AdminLoginAsync(LoginRequest request);
    Task<LoginResponse> UserPasswordLoginAsync(LoginRequest request);
    Task<LoginResponse> WechatLoginAsync(WechatLoginRequest request);
    string GenerateJwtToken(string identifier, string role, string displayName);
}
