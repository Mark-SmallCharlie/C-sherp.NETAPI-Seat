using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WebApplication1.Data;
using WebApplication1.Models.DTOs.Requests;
using WebApplication1.Models.DTOs.Responses;
using WebApplication1.Models.Entities;
using WebApplication1.Services.Interfaces;

namespace WebApplication1.API.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IUserService _userService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext context, IConfiguration configuration,
        IUserService userService, ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _userService = userService;
        _logger = logger;
    }

    public async Task<LoginResponse> AdminLoginAsync(LoginRequest request)
    {
        try
        {
            _logger.LogInformation("管理员登录尝试 - 用户名: {Username}", request.Username);

            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.LogWarning("管理员登录 - 用户名或密码为空");
                return new LoginResponse { Success = false, Message = "用户名和密码不能为空" };
            }

            var adminUser = await _context.AdminUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Username == request.Username && a.IsActive);

            if (adminUser == null)
            {
                _logger.LogWarning("管理员登录失败 - 用户不存在或未激活: {Username}", request.Username);
                return new LoginResponse { Success = false, Message = "用户名或密码错误" };
            }

            if (!VerifyPassword(request.Password, adminUser.PasswordHash))
            {
                _logger.LogWarning("管理员登录失败 - 密码错误: {Username}", request.Username);
                return new LoginResponse { Success = false, Message = "用户名或密码错误" };
            }

            var token = GenerateJwtToken(adminUser.Username, "Admin", adminUser.DisplayName);

            _logger.LogInformation("管理员登录成功 - 用户名: {Username}", request.Username);

            return new LoginResponse
            {
                Success = true,
                Token = token,
                UserInfo = new UserInfoResponse
                {
                    Id = adminUser.Id,
                    DisplayName = adminUser.DisplayName,
                    Role = "Admin"
                },
                Message = "登录成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "管理员登录处理失败 - 用户名: {Username}", request.Username);
            return new LoginResponse { Success = false, Message = "登录处理失败" };
        }
    }

    public async Task<LoginResponse> WechatLoginAsync(WechatLoginRequest request)
    {
        try
        {
            _logger.LogInformation("微信登录尝试 - OpenId: {OpenId}, 昵称: {NickName}",
                request.OpenId ?? "未提供", request.NickName);

            if (string.IsNullOrWhiteSpace(request.Code) && string.IsNullOrWhiteSpace(request.OpenId))
            {
                _logger.LogWarning("微信登录失败 - Code 和 OpenId 都为空");
                return new LoginResponse { Success = false, Message = "登录凭证不能为空" };
            }

            // 简化版：实际项目中应该调用微信API验证code并获取OpenId
            var openId = await GetOpenIdFromWechatAsync(request.Code, request.OpenId);

            if (string.IsNullOrWhiteSpace(openId))
            {
                _logger.LogWarning("微信登录失败 - 获取OpenId失败");
                return new LoginResponse { Success = false, Message = "微信登录失败" };
            }

            var user = await _userService.GetUserByOpenIdAsync(openId);

            if (user == null)
            {
                // 新用户，创建待审核账户
                _logger.LogInformation("创建新用户 - OpenId: {OpenId}", openId);
                user = await _userService.CreateUserAsync(openId, request.NickName, request.AvatarUrl);
            }

            var requiresApproval = user.Role == UserRole.Pending;
            string token = string.Empty;
            UserInfoResponse? userInfo = null;

            if (!requiresApproval)
            {
                token = GenerateJwtToken(user.OpenId, user.Role.ToString(), user.NickName);
                userInfo = new UserInfoResponse
                {
                    Id = user.Id,
                    NickName = user.NickName,
                    AvatarUrl = user.AvatarUrl,
                    Role = user.Role.ToString(),
                    DisplayName = user.NickName
                };

                _logger.LogInformation("微信登录成功 - 用户: {NickName} (ID: {UserId})", user.NickName, user.Id);
            }
            else
            {
                _logger.LogInformation("用户待审核 - 用户: {NickName} (ID: {UserId})", user.NickName, user.Id);
            }

            return new LoginResponse
            {
                Success = true,
                Token = token,
                UserInfo = userInfo,
                RequiresApproval = requiresApproval,
                Message = requiresApproval ? "账号待审核，请联系管理员" : "登录成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "微信登录处理失败");
            return new LoginResponse { Success = false, Message = "登录处理失败" };
        }
    }

    public string GenerateJwtToken(string identifier, string role, string displayName)
    {
        try
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "YourSuperSecretKeyForJWTTokenGeneration12345";
            var issuer = _configuration["Jwt:Issuer"] ?? "WebApplication1API";
            var audience = _configuration["Jwt:Audience"] ?? "WebApplication1Client";
            var expiryHours = _configuration.GetValue<int>("Jwt:ExpiryHours", 24);

            if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 16)
            {
                throw new ArgumentException("JWT密钥长度不足，至少需要16个字符");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, identifier),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Name, displayName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expiryHours),
                signingCredentials: credentials);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            _logger.LogDebug("JWT Token生成成功 - 用户: {DisplayName}, 角色: {Role}", displayName, role);

            return tokenString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成JWT Token失败");
            throw;
        }
    }

    private async Task<string?> GetOpenIdFromWechatAsync(string? code, string? openId)
    {
        // 如果有code，调用微信API获取真实的OpenId
        if (!string.IsNullOrWhiteSpace(code))
        {
            try
            {
                var appId = _configuration["WeChatMiniProgram:AppId"];
                var appSecret = _configuration["WeChatMiniProgram:AppSecret"];
                var apiUrl = _configuration["WeChatMiniProgram:Jscode2SessionUrl"]
                     ?? "https://api.weixin.qq.com/sns/jscode2session";

                if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(appSecret))
                {
                    _logger.LogError("微信小程序配置不完整，请检查AppId和AppSecret");
                    return null;
                }

                // 构建微信API请求URL
                var requestUrl = $"{apiUrl}?appid={appId}&secret={appSecret}&js_code={code}&grant_type=authorization_code";

                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<WeChatSessionResult>(content);

                    if (result != null && result.ErrorCode == 0 && !string.IsNullOrWhiteSpace(result.OpenId))
                    {
                        _logger.LogInformation("微信API调用成功，获取到OpenId: {OpenId}", result.OpenId);
                        return result.OpenId;
                    }
                    else
                    {
                        _logger.LogError("微信API返回错误: {ErrorCode} - {ErrorMessage}",
                            result?.ErrorCode, result?.ErrorMessage);
                        return null;
                    }
                }
                else
                {
                    _logger.LogError("微信API请求失败: {StatusCode}", response.StatusCode);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "调用微信API获取OpenId时发生异常");
                return null;
            }
        }

        return null;
    }

    private bool VerifyPassword(string password, string storedHash)
    {
        try
        {
            using var sha256 = SHA256.Create();
            var hashedPassword = Convert.ToHexString(
                sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));

            return string.Equals(hashedPassword, storedHash, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "密码验证失败");
            return false;
        }
    }

    public bool ValidateToken(string token)
    {
        try
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "YourSuperSecretKeyForJWTTokenGeneration12345";
            var issuer = _configuration["Jwt:Issuer"] ?? "WebApplication1API";
            var audience = _configuration["Jwt:Audience"] ?? "WebApplication1Client";

            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };

            tokenHandler.ValidateToken(token, validationParameters, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
