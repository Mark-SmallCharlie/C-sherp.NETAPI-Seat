using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models.Entities;
using WebApplication1.Services.Interfaces;

namespace WebApplication1.Services;

/// <summary>
/// 候补服务实现 — 管理候补队列的加入、确认、取消、推进等操作。
/// </summary>
public class WaitlistService : IWaitlistService
{
    private readonly AppDbContext _context;
    private readonly ILogger<WaitlistService> _logger;
    private readonly INotificationService _notificationService;
    private readonly IReservationService _reservationService;

    public WaitlistService(AppDbContext context, ILogger<WaitlistService> logger,
        INotificationService notificationService, IReservationService reservationService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
        _reservationService = reservationService;
    }

    public async Task<WaitlistEntry?> JoinWaitlistAsync(int userId, int seatNumber, DateTime startTime, DateTime endTime)
    {
        // 检查用户是否已在该座位+时段排队
        var existing = await _context.WaitlistEntries
            .FirstOrDefaultAsync(w =>
                w.UserId == userId
                && w.SeatNumber == seatNumber
                && w.StartTime == startTime
                && w.EndTime == endTime
                && (w.Status == WaitlistStatus.Waiting || w.Status == WaitlistStatus.Notified));

        if (existing != null)
        {
            _logger.LogWarning("用户 {UserId} 已在座位 {SeatNumber} 的候补队列中", userId, seatNumber);
            return null;
        }

        // 计算排队位置
        var maxPosition = await _context.WaitlistEntries
            .Where(w => w.SeatNumber == seatNumber && w.StartTime == startTime && w.EndTime == endTime
                && w.Status == WaitlistStatus.Waiting)
            .MaxAsync(w => (int?)w.QueuePosition) ?? 0;

        var entry = new WaitlistEntry
        {
            UserId = userId,
            SeatNumber = seatNumber,
            StartTime = startTime,
            EndTime = endTime,
            Status = WaitlistStatus.Waiting,
            QueuePosition = maxPosition + 1,
            CreatedAt = DateTime.UtcNow
        };

        _context.WaitlistEntries.Add(entry);
        await _context.SaveChangesAsync();

        _logger.LogInformation("用户 {UserId} 加入候补 - 座位: {SeatNumber}, 位置: {QueuePosition}, 时间: {StartTime}~{EndTime}",
            userId, seatNumber, entry.QueuePosition, startTime, endTime);

        return entry;
    }

    public async Task<WaitlistEntry?> ConfirmWaitlistAsync(int waitlistId, int userId)
    {
        var entry = await _context.WaitlistEntries
            .FirstOrDefaultAsync(w => w.Id == waitlistId && w.UserId == userId);

        if (entry == null || entry.Status != WaitlistStatus.Notified)
        {
            _logger.LogWarning("候补确认失败 - WaitlistId: {Id}, 用户: {UserId}, 状态: {Status}",
                waitlistId, userId, entry?.Status);
            return null;
        }

        // 检查是否超时
        if (entry.ConfirmDeadline.HasValue && entry.ConfirmDeadline.Value < DateTime.UtcNow)
        {
            entry.Status = WaitlistStatus.Expired;
            await _context.SaveChangesAsync();
            _logger.LogWarning("候补确认已超时 - WaitlistId: {Id}", waitlistId);
            return null;
        }

        // 再次检查座位冲突
        var hasConflict = await _reservationService.CheckSeatConflictAsync(
            entry.SeatNumber, entry.StartTime, entry.EndTime);

        if (hasConflict)
        {
            entry.Status = WaitlistStatus.Expired;
            await _context.SaveChangesAsync();
            _logger.LogWarning("候补确认时座位仍有冲突 - WaitlistId: {Id}, 座位: {SeatNumber}", waitlistId, entry.SeatNumber);
            return null;
        }

        // 创建预约
        var request = new Models.DTOs.Requests.CreateReservationRequest
        {
            SeatNumber = entry.SeatNumber,
            StartTime = entry.StartTime,
            EndTime = entry.EndTime
        };

        var reservation = await _reservationService.CreateReservationAsync(request, userId);
        if (reservation != null)
        {
            entry.Status = WaitlistStatus.Confirmed;
            await _context.SaveChangesAsync();
            _logger.LogInformation("候补确认成功 - WaitlistId: {Id}, 预约ID: {ReservationId}", waitlistId, reservation.Id);
        }

        return entry;
    }

    public async Task<bool> CancelWaitlistAsync(int waitlistId, int userId)
    {
        var entry = await _context.WaitlistEntries
            .FirstOrDefaultAsync(w => w.Id == waitlistId && w.UserId == userId);

        if (entry == null || entry.Status == WaitlistStatus.Confirmed || entry.Status == WaitlistStatus.Expired)
            return false;

        entry.Status = WaitlistStatus.Cancelled;
        await _context.SaveChangesAsync();

        _logger.LogInformation("用户 {UserId} 取消了候补 - WaitlistId: {Id}, 座位: {SeatNumber}", userId, waitlistId, entry.SeatNumber);
        return true;
    }

    public async Task<WaitlistEntry?> GetUserWaitlistAsync(int userId, int seatNumber, DateTime startTime, DateTime endTime)
    {
        return await _context.WaitlistEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(w =>
                w.UserId == userId
                && w.SeatNumber == seatNumber
                && w.StartTime == startTime
                && w.EndTime == endTime
                && (w.Status == WaitlistStatus.Waiting || w.Status == WaitlistStatus.Notified));
    }

    public async Task PromoteWaitlistAsync(int seatNumber, DateTime startTime, DateTime endTime)
    {
        // 找到该座位+时段下第一个 Waiting 状态的候补者
        var firstInLine = await _context.WaitlistEntries
            .OrderBy(w => w.QueuePosition)
            .FirstOrDefaultAsync(w =>
                w.SeatNumber == seatNumber
                && w.StartTime == startTime
                && w.EndTime == endTime
                && w.Status == WaitlistStatus.Waiting);

        if (firstInLine == null)
            return;

        var now = DateTime.UtcNow;
        var deadline = now.AddMinutes(15);

        firstInLine.Status = WaitlistStatus.Notified;
        firstInLine.NotifiedAt = now;
        firstInLine.ConfirmDeadline = deadline;

        await _context.SaveChangesAsync();

        // 发送通知
        await _notificationService.CreateNotificationAsync(
            firstInLine.UserId,
            "候补名额可用",
            $"您候补的座位 {seatNumber}（{startTime:yyyy-MM-dd HH:mm} 至 {endTime:yyyy-MM-dd HH:mm}）现有名额可用，请在 15 分钟内确认，超时将顺延至下一位。",
            NotificationType.WaitlistAvailable,
            null);

        _logger.LogInformation("候补队列推进 - 座位: {SeatNumber}, 通知用户: {UserId}, 截止: {Deadline}",
            seatNumber, firstInLine.UserId, deadline);
    }
}
