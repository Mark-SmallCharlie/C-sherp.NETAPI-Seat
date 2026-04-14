//此类做为微信小程序注册类,负责处理用户注册请求，调用用户服务进行注册逻辑处理，并返回相应的结果。
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApplication1.Models.DTOs.Requests;
using WebApplication1.Models.DTOs.Responses;
using WebApplication1.Controllers;
using WebApplication1.Services.Interfaces;

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
        Console.WriteLine($"Register result: Success={result.Success}, Message={result.Message}, UserId={result.UserId}");
        if (!result.Success)
        {
            return BadRequest(result.Message);
        }

        return Ok(result);
    }
}
