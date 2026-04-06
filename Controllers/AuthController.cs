using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models.DTOs.Requests;
using WebApplication1.Services.Interfaces;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseController
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// 统一账号密码登录：先匹配 AdminUsers，再匹配 Users（带 PasswordHash 的注册用户）。
    /// 小程序可只调用此接口，无需区分管理员与普通用户。
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequestResponse("请求数据无效");
            }

            var adminResult = await _authService.AdminLoginAsync(request);
            if (adminResult.Success)
            {
                return OkResponse(
                    new { token = adminResult.Token, userInfo = adminResult.UserInfo },
                    adminResult.Message ?? "登录成功");
            }

            var userResult = await _authService.UserPasswordLoginAsync(request);
            if (userResult.Success)
            {
                return OkResponse(
                    new { token = userResult.Token, userInfo = userResult.UserInfo },
                    userResult.Message ?? "登录成功");
            }

            var errMsg = userResult.Message ?? adminResult.Message ?? "用户名或密码错误";
            return UnauthorizedResponse(errMsg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登录异常: {Username}", request.Username);
            return ServerErrorResponse("登录处理失败");
        }
    }

    [HttpPost("admin-login")]
    public async Task<IActionResult> AdminLogin([FromBody] LoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequestResponse("请求数据无效");
            }

            var result = await _authService.AdminLoginAsync(request);

            if (!result.Success)
            {
                return UnauthorizedResponse(result.Message ?? "用户名或密码错误");
            }

            _logger.LogInformation("管理员登录成功: {Username}", request.Username);
            return OkResponse(new { result.Token, result.UserInfo }, result.Message ?? "登录成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "管理员登录异常: {Username}", request.Username);
            return ServerErrorResponse("登录处理失败");
        }
    }

    [HttpPost("wechat-login")]
    public async Task<IActionResult> WechatLogin([FromBody] WechatLoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequestResponse("请求数据无效");
            }

            var result = await _authService.WechatLoginAsync(request);

            if (!result.Success)
            {
                return BadRequestResponse(result.Message);
            }

            if (result.RequiresApproval)
            {
                _logger.LogInformation("微信用户待审核: {NickName}", request.NickName);
                return OkResponse(new { result.RequiresApproval }, result.Message);
            }

            _logger.LogInformation("微信登录成功: {NickName}", request.NickName);
            return OkResponse(new { result.Token, result.UserInfo, result.RequiresApproval }, result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "微信登录异常: {NickName}", request.NickName);
            return ServerErrorResponse("微信登录处理失败");
        }
    }

    [HttpGet("validate-token")]
    [Authorize]
    public IActionResult ValidateToken()
    {
        try
        {
            var userId = GetUserId();
            var userRole = GetUserRole();

            if (userId == null)
            {
                return UnauthorizedResponse("Token无效");
            }

            _logger.LogInformation("Token验证成功 - 用户ID: {UserId}, 角色: {Role}", userId, userRole);
            return OkResponse(new { userId, userRole }, "Token有效");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token验证异常");
            return UnauthorizedResponse("Token验证失败");
        }
    }

    [HttpGet("admin-only")]
    [Authorize(Roles = "Admin")]
    public IActionResult AdminOnlyEndpoint()
    {
        return OkResponse(new { message = "这是管理员专属接口" }, "访问成功");
    }
}
