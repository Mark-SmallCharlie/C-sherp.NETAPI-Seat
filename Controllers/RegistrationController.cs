//此类做为微信小程序注册类，但存在RegisterRequest命名空间未找到的错误，故暂时注释掉

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApplication1.Models.DTOs.Requests;
using WebApplication1.Models.DTOs.Responses;
using WebApplication1.Controllers;
using WebApplication1.Services.Interfaces;
using WebApplication1.Models.DTOs.Requests; // 添加此 using 指令

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegistrationController : ControllerBase
{
    private readonly IUserService _userService;

    public RegistrationController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _userService.RegisterAsync(request);
        if (!result.Success)
        {
            return BadRequest(result.Message);
        }

        return Ok(result);
    }
}
