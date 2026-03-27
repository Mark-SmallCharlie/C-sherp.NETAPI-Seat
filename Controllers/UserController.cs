using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using WebApplication1.Controllers;
using WebApplication1.Models.Entities;
using WebApplication1.Services.Interfaces;
using WebApplication1.Services;
using WebApplication1.Models.DTOs;


namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : BaseController
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return UnauthorizedResponse("用户未认证");
            }

            var user = await _userService.GetUserByIdAsync(userId.Value);
            if (user == null)
            {
                return NotFoundResponse("用户不存在");
            }

            _logger.LogInformation("获取用户资料 - 用户ID: {UserId}", userId);
            return OkResponse(user, "获取用户资料成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户资料异常 - 用户ID: {UserId}", GetUserId());
            return ServerErrorResponse("获取用户资料失败");
        }
    }

    [HttpGet("pending-users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPendingUsers()
    {
        try
        {
            var pendingUsers = await _userService.GetPendingUsersAsync();

            _logger.LogInformation("获取待审核用户列表 - 数量: {Count}", pendingUsers.Count);
            return OkResponse(pendingUsers, "获取待审核用户列表成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取待审核用户列表异常");
            return ServerErrorResponse("获取待审核用户列表失败");
        }
    }

    [HttpGet("all-users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var users = await _userService.GetAllUsersAsync();

            _logger.LogInformation("获取所有用户列表 - 数量: {Count}", users.Count);
            return OkResponse(users, "获取用户列表成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有用户列表异常");
            return ServerErrorResponse("获取用户列表失败");
        }
    }

    [HttpPost("approve-user/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveUser(int userId, [FromBody] ApproveUserRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequestResponse("请求数据无效");
            }

            var success = await _userService.ApproveUserAsync(userId, request.IsApproved, request.Note);

            if (!success)
            {
                return BadRequestResponse("审核用户失败，用户不存在或状态不正确");
            }

            var action = request.IsApproved ? "通过" : "拒绝";
            _logger.LogInformation("用户审核{Action} - 用户ID: {UserId}", action, userId);

            return OkResponse<object>(null, $"用户审核{action}成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "审核用户异常 - 用户ID: {UserId}", userId);
            return ServerErrorResponse("审核用户失败");
        }
    }

    [HttpPut("update-role/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUserRole(int userId, [FromBody] UpdateRoleRequest request)
    {
        try
        {
            if (!ModelState.IsValid || !Enum.IsDefined(typeof(UserRole), request.NewRole))
            {
                return BadRequestResponse("请求数据无效");
            }

            var success = await _userService.UpdateUserRoleAsync(userId, request.NewRole);

            if (!success)
            {
                return BadRequestResponse("更新用户角色失败，用户不存在");
            }

            _logger.LogInformation("更新用户角色 - 用户ID: {UserId}, 新角色: {NewRole}", userId, request.NewRole);
            return OkResponse<object>(null, "更新用户角色成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户角色异常 - 用户ID: {UserId}", userId);
            return ServerErrorResponse("更新用户角色失败");
        }
    }
}

// DTOs for UserController
public class ApproveUserRequest
{
    public bool IsApproved { get; set; }
    public string? Note { get; set; }
}

public class UpdateRoleRequest
{
    public UserRole NewRole { get; set; }
}
