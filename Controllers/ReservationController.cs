using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; 
using WebApplication1.Controllers;  
using WebApplication1.Models.DTOs.Requests;
using WebApplication1.Services.Interfaces;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReservationController : BaseController
{
    private readonly IReservationService _reservationService;
    private readonly ILogger<ReservationController> _logger;

    public ReservationController(IReservationService reservationService, ILogger<ReservationController> logger)
    {
        _reservationService = reservationService;
        _logger = logger;
    }

    [HttpPost("create")]
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
                return BadRequestResponse("创建预约失败，可能是时间冲突或座位无效");
            }

            _logger.LogInformation("创建预约成功 - 预约ID: {ReservationId}, 用户ID: {UserId}",
                reservation.Id, userId);

            return OkResponse(reservation, "预约创建成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建预约异常 - 用户ID: {UserId}", GetUserId());
            return ServerErrorResponse("创建预约失败");
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

            var success = await _reservationService.CancelReservationAsync(reservationId, userId.Value, isAdmin, adminNote);

            if (!success)
            {
                return BadRequestResponse("取消预约失败，预约不存在或无权操作");
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
    [Authorize(Roles = "Admin")]
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
}

// DTOs for ReservationController
public class CancelReservationRequest
{
    public string? AdminNote { get; set; }
}

public class CheckConflictRequest
{
    public int SeatNumber { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int? ExcludeReservationId { get; set; }
}
