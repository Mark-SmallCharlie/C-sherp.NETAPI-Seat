using WebApplication1.Models.DTOs.Requests;
using WebApplication1.Models.DTOs.Responses;

namespace WebApplication1.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> AdminLoginAsync(LoginRequest request);
    Task<LoginResponse> WechatLoginAsync(WechatLoginRequest request);
    string GenerateJwtToken(string identifier, string role, string displayName);
}
