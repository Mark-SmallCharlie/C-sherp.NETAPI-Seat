using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models.DTOs.Requests;
using WebApplication1.Services.Interfaces;

namespace WebApplication1.Controllers;

/// <summary>
/// 微信小程序注册控制器，负责处理用户注册请求。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RegistrationController : BaseController
{
    private readonly IUserService _userService;
    private readonly ILogger<RegistrationController> _logger;

    public RegistrationController(IUserService userService, ILogger<RegistrationController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequestResponse("请求数据无效");
            }

            var result = await _userService.RegisterAsync(request);
            _logger.LogInformation("注册结果 - Success: {Success}, Message: {Message}, UserId: {UserId}",
                result.Success, result.Message, result.UserId);

            if (!result.Success)
            {
                return BadRequestResponse(result.Message);
            }

            return OkResponse(result, result.Message ?? "注册成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册异常 - OpenId: {OpenId}", request.OpenId);
            return ServerErrorResponse("注册处理失败");
        }
    }
}
