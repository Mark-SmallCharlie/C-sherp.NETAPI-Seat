using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using WebApplication1.Controllers;
using WebApplication1.Models.DTOs.Requests;
using WebApplication1.Services.Interfaces;
/**
  ReservationController是一个ASP.NET Core Web API控制器，
负责处理与预约相关的HTTP请求。它提供了创造预约、取消预约、获取用户预约列表、
获取所有预约列表、获取活跃预约列表以及检查座位冲突等功能。
该控制器使用依赖注入来获取预约服务和日志记录器，
并通过授权属性确保只有认证用户才能访问这些端点。
每个方法都包含错误处理和日志记录，以便更好地跟踪操作和调试问题。
 */
namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReservationController : BaseController
{
    private readonly IReservationService _reservationService;
    private readonly IWaitlistService _waitlistService;
    private readonly ILogger<ReservationController> _logger;

    public ReservationController(IReservationService reservationService, IWaitlistService waitlistService, ILogger<ReservationController> logger)
    {
        _reservationService = reservationService;
        _waitlistService = waitlistService;
        _logger = logger;
    }

    [HttpPost("create")]
    [EnableRateLimiting("ReservationPolicy")]
    public async Task<IActionResult> CreateReservation([FromBody] CreateReservationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequestResponse("请求数据无效");
            }

            var userId = GetUserId();
            if (userId == null)
            {
                return UnauthorizedResponse("用户未认证");
            }

            var reservation = await _reservationService.CreateReservationAsync(request, userId.Value);

            if (reservation == null)
            {
                return BadRequestResponse("创造预约失败，可能是时间冲突或座位无效");
            }

            _logger.LogInformation("创造预约成功 - 预约ID: {ReservationId}, 用户ID: {UserId}",
                reservation.Id, userId);

            return OkResponse(reservation, "预约创造成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创造预约异常 - 用户ID: {UserId}", GetUserId());
            return ServerErrorResponse("创造预约失败");
        }
    }

    [HttpPost("cancel/{reservationId}")]
    public async Task<IActionResult> CancelReservation(int reservationId, [FromBody] CancelReservationRequest? request = null)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return UnauthorizedResponse("用户未认证");
            }

            var isAdmin = IsAdmin();
            var adminNote = isAdmin ? request?.AdminNote : null;

            // 先获取预约信息（用于候补推进）
            var reservations = await _reservationService.GetUserReservationsAsync(userId.Value);
            var target = reservations.FirstOrDefault(r => r.Id == reservationId);

            var success = await _reservationService.CancelReservationAsync(reservationId, userId.Value, isAdmin, adminNote);

            if (!success)
            {
                return BadRequestResponse("取消预约失败，预约不存在或无权操作");
            }

            // 取消成功后推进候补队列
            if (target != null)
            {
                await _waitlistService.PromoteWaitlistAsync(target.SeatNumber, target.StartTime, target.EndTime);
            }

            _logger.LogInformation("取消预约成功 - 预约ID: {ReservationId}, 操作人: {UserId}, 是否管理员: {IsAdmin}",
                reservationId, userId, isAdmin);

            return OkResponse<object>(null, "预约取消成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消预约异常 - 预约ID: {ReservationId}", reservationId);
            return ServerErrorResponse("取消预约失败");
        }
    }

    [HttpGet("my-reservations")]
    public async Task<IActionResult> GetMyReservations()
    {
        try
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return UnauthorizedResponse("用户未认证");
            }

            var reservations = await _reservationService.GetUserReservationsAsync(userId.Value);

            _logger.LogInformation("获取用户预约列表 - 用户ID: {UserId}, 数量: {Count}",
                userId, reservations.Count);

            return OkResponse(reservations, "获取预约列表成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户预约列表异常 - 用户ID: {UserId}", GetUserId());
            return ServerErrorResponse("获取预约列表失败");
        }
    }

    [HttpGet("all-reservations")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllReservations()
    {
        try
        {
            var reservations = await _reservationService.GetAllReservationsAsync();

            _logger.LogInformation("获取所有预约列表 - 数量: {Count}", reservations.Count);
            return OkResponse(reservations, "获取所有预约列表成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有预约列表异常");
            return ServerErrorResponse("获取预约列表失败");
        }
    }

    [HttpGet("active-reservations")]
    //[Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetActiveReservations()
    {
        try
        {
            var reservations = await _reservationService.GetActiveReservationsAsync();

            _logger.LogInformation("获取活跃预约列表 - 数量: {Count}", reservations.Count);
            return OkResponse(reservations, "获取活跃预约列表成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活跃预约列表异常");
            return ServerErrorResponse("获取活跃预约列表失败");
        }
    }

    [HttpPost("check-conflict")]
    public async Task<IActionResult> CheckSeatConflict([FromBody] CheckConflictRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequestResponse("请求数据无效");
            }

            var hasConflict = await _reservationService.CheckSeatConflictAsync(
                request.SeatNumber, request.StartTime, request.EndTime, request.ExcludeReservationId);

            _logger.LogInformation("检查座位冲突 - 座位: {SeatNumber}, 时间: {StartTime} 到 {EndTime}, 冲突: {HasConflict}",
                request.SeatNumber, request.StartTime, request.EndTime, hasConflict);

            return OkResponse(new { hasConflict }, hasConflict ? "存在时间冲突" : "无时间冲突");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查座位冲突异常");
            return ServerErrorResponse("检查座位冲突失败");
        }
    }

    [HttpPost("temp-leave/{reservationId}")]
    public async Task<IActionResult> SetTemporaryLeave(int reservationId, [FromQuery] int minutes = 15)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return UnauthorizedResponse("用户未认证");
            }

            var success = await _reservationService.SetTemporaryLeaveAsync(reservationId, userId.Value, minutes);
            if (!success)
            {
                // 可以是找不到，或者不在 Active 状态，或者不是该用户的预约
                return BadRequestResponse("设置暂离失败，可能是预约不存在、已结束或非本人操作");
            }

            return OkResponse<object>(null, $"成功设置暂离 {minutes} 分钟");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置暂离异常");
            return ServerErrorResponse("设置暂离失败");
        }
    }

    [HttpPost("return-leave/{reservationId}")]
    public async Task<IActionResult> ReturnFromLeave(int reservationId)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return UnauthorizedResponse("用户未认证");
            }

            var success = await _reservationService.ReturnFromLeaveAsync(reservationId, userId.Value);
            if (!success)
            {
                return BadRequestResponse("结束暂离失败，可能是预约不存在或非本人操作");
            }

            return OkResponse<object>(null, "已成功取消暂离状态，欢迎回归座位");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "结束暂离异常");
            return ServerErrorResponse("结束暂离失败");
        }
    }

    // ========== 候补相关接口 ==========

    /// <summary>加入候补队列</summary>
    [HttpPost("join-waitlist")]
    public async Task<IActionResult> JoinWaitlist([FromBody] CheckConflictRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return UnauthorizedResponse("用户未认证");

            if (!ModelState.IsValid)
                return BadRequestResponse("请求数据无效");

            // 确认当前确实有冲突
            var hasConflict = await _reservationService.CheckSeatConflictAsync(
                request.SeatNumber, request.StartTime, request.EndTime);

            if (!hasConflict)
                return BadRequestResponse("该座位时段当前无冲突，可直接预约");

            var entry = await _waitlistService.JoinWaitlistAsync(
                userId.Value, request.SeatNumber, request.StartTime, request.EndTime);

            if (entry == null)
                return BadRequestResponse("加入候补失败，可能已在队列中");

            _logger.LogInformation("用户 {UserId} 加入候补 - 座位: {SeatNumber}", userId, request.SeatNumber);
            return OkResponse(entry, $"已加入候补队列，排在第 {entry.QueuePosition} 位");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加入候补异常");
            return ServerErrorResponse("加入候补失败");
        }
    }

    /// <summary>确认候补名额</summary>
    [HttpPost("confirm-waitlist/{waitlistId}")]
    public async Task<IActionResult> ConfirmWaitlist(int waitlistId)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return UnauthorizedResponse("用户未认证");

            var entry = await _waitlistService.ConfirmWaitlistAsync(waitlistId, userId.Value);

            if (entry == null || entry.Status != Models.Entities.WaitlistStatus.Confirmed)
                return BadRequestResponse("确认失败，候补可能已过期、超时或座位已被预约");

            return OkResponse<object>(null, "候补确认成功，已自动创建预约");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "确认候补异常");
            return ServerErrorResponse("确认候补失败");
        }
    }

    /// <summary>取消候补</summary>
    [HttpPost("cancel-waitlist/{waitlistId}")]
    public async Task<IActionResult> CancelWaitlist(int waitlistId)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return UnauthorizedResponse("用户未认证");

            var ok = await _waitlistService.CancelWaitlistAsync(waitlistId, userId.Value);
            if (!ok) return BadRequestResponse("取消失败，候补不存在或状态不允许取消");

            return OkResponse<object>(null, "已取消候补");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消候补异常");
            return ServerErrorResponse("取消候补失败");
        }
    }
}

