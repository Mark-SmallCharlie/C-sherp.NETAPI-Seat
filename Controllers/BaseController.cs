using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace WebApplication1.Controllers;
/**
 BaseController 是所有 API 控制器的基类，提供了通用的用户信息获取方法和统一的响应格式。它包含以下功能：
   1. 获取当前用户的 ID 和角色信息，方便子类控制器进行权限判断。
   2. 提供统一的成功响应、错误响应、未找到响应、未授权响应和服务器错误响应方法，简化子类控制器的代码。
   3. 通过使用 [ApiController] 和 [Route] 特性，确保所有继承自 BaseController 的控制器都遵循 RESTful API 的设计规范。
   4. 通过 IsAdmin 方法，子类控制器可以轻松判断当前用户是否具有管理员权限，从而实现基于角色的访问控制。
   5. 通过 GetUserId 和 GetUserRole 方法，子类控制器可以获取当前用户的 ID 和角色信息，方便进行业务逻辑处理。
   6. 通过统一的响应格式，前端可以更方便地处理 API 响应，无需关心具体的 HTTP 状态码和响应结构。
   7. 通过继承 BaseController，子类控制器可以专注于业务逻辑的实现，而无需重复编写用户信息获取和响应处理的代码
 */
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
