using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BaseController : ControllerBase
{
    protected int? GetUserId()
    {
        var userIdClaim = User.FindFirst("nameid") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
        {
            return userId;
        }
        return null;
    }

    protected string? GetUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value;
    }

    protected bool IsAdmin()
    {
        return GetUserRole() == "Admin";
    }

    protected IActionResult OkResponse<T>(T data, string message = "操作成功")
    {
        return Ok(new { success = true, message, data });
    }

    protected IActionResult BadRequestResponse(string message = "请求无效")
    {
        return BadRequest(new { success = false, message });
    }

    protected IActionResult NotFoundResponse(string message = "资源未找到")
    {
        return NotFound(new { success = false, message });
    }

    protected IActionResult UnauthorizedResponse(string message = "未授权访问")
    {
        return Unauthorized(new { success = false, message });
    }

    protected IActionResult ServerErrorResponse(string message = "服务器内部错误")
    {
        return StatusCode(500, new { success = false, message });
    }
}
