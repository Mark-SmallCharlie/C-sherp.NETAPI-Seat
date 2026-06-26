using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services.Interfaces;

namespace WebApplication1.Controllers;

/// <summary>
/// 通知控制器 — 用户查看、标记已读自己的通知。
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : BaseController
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>获取当前用户的通知列表</summary>
    [HttpGet("my-notifications")]
    public async Task<IActionResult> GetMyNotifications([FromQuery] bool unreadOnly = false)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return UnauthorizedResponse("用户未认证");

            var notifications = await _notificationService.GetUserNotificationsAsync(userId.Value, unreadOnly);
            var unreadCount = await _notificationService.GetUnreadCountAsync(userId.Value);

            return OkResponse(new { notifications, unreadCount }, "获取通知列表成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取通知列表异常");
            return ServerErrorResponse("获取通知列表失败");
        }
    }

    /// <summary>获取未读通知数量</summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return UnauthorizedResponse("用户未认证");

            var count = await _notificationService.GetUnreadCountAsync(userId.Value);
            return OkResponse(new { count }, "获取未读数量成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取未读数量异常");
            return ServerErrorResponse("获取未读数量失败");
        }
    }

    /// <summary>标记单条通知为已读</summary>
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return UnauthorizedResponse("用户未认证");

            var ok = await _notificationService.MarkAsReadAsync(id, userId.Value);
            if (!ok) return NotFoundResponse("通知不存在或不属于当前用户");

            return OkResponse<object>(null, "已标记为已读");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标记已读异常");
            return ServerErrorResponse("标记已读失败");
        }
    }

    /// <summary>标记全部通知为已读</summary>
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return UnauthorizedResponse("用户未认证");

            var count = await _notificationService.MarkAllAsReadAsync(userId.Value);
            return OkResponse(new { markedCount = count }, $"已标记 {count} 条通知为已读");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "全部标记已读异常");
            return ServerErrorResponse("全部标记已读失败");
        }
    }
}
