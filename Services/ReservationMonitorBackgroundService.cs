using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebApplication1.Data;
using WebApplication1.Models.Entities;
using WebApplication1.Services.Interfaces;

namespace WebApplication1.Services
{
    public class ReservationMonitorBackgroundService : BackgroundService
    {
        private readonly ILogger<ReservationMonitorBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ReservationMonitorBackgroundService(ILogger<ReservationMonitorBackgroundService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("预约监控服务已启动（含超时释放、暂离预警、封禁解封、候补超时处理）...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
                        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        // 1. 超时释放
                        int releasedCount = await reservationService.ReleaseTimeoutReservationsAsync();
                        if (releasedCount > 0)
                        {
                            _logger.LogInformation("成功释放了 {Count} 个超时无人使用的座位。", releasedCount);
                        }

                        // 2. 暂离预警：检查 LeaveEndTime 在 5 分钟内到期的预约
                        await CheckLeaveExpiringAsync(dbContext, notificationService);

                        // 3. 封禁解封检查
                        await CheckBanExpiryAsync(dbContext, notificationService);

                        // 4. 候补超时处理
                        await HandleWaitlistTimeoutsAsync(dbContext, notificationService, scope);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "预约监控服务发生异常。");
                }

                // 每 1 分钟执行一次检查（更频繁以支持暂离预警和候补超时）
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        /// <summary>暂离即将超时预警（剩余 5 分钟内）</summary>
        private async Task CheckLeaveExpiringAsync(AppDbContext dbContext, INotificationService notificationService)
        {
            try
            {
                var now = DateTime.UtcNow;
                var warningThreshold = now.AddMinutes(5);

                var expiringReservations = await dbContext.Reservations
                    .Include(r => r.User)
                    .Where(r => r.Status == ReservationStatus.Active
                        && r.LeaveEndTime.HasValue
                        && r.LeaveEndTime.Value > now
                        && r.LeaveEndTime.Value <= warningThreshold)
                    .ToListAsync();

                foreach (var reservation in expiringReservations)
                {
                    if (reservation.User != null)
                    {
                        _logger.LogInformation("暂离预警 - 预约 {Id}, 用户 {UserId}, 将于 {LeaveEndTime} 到期",
                            reservation.Id, reservation.User.Id, reservation.LeaveEndTime);

                        await notificationService.CreateNotificationAsync(
                            reservation.User.Id,
                            "暂离即将超时",
                            $"您的座位 {reservation.SeatNumber} 暂离时间还剩不到5分钟，请尽快返回座位，以免被自动释放。",
                            NotificationType.TimeoutWarning,
                            reservation.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "暂离预警检查失败");
            }
        }

        /// <summary>检查封禁到期并通知用户</summary>
        private async Task CheckBanExpiryAsync(AppDbContext dbContext, INotificationService notificationService)
        {
            try
            {
                var now = DateTime.UtcNow;

                // 查找封禁刚刚到期的用户（SuspendedUntil 在过去但用户仍处于封禁状态）
                var recentlyUnbanned = await dbContext.Users
                    .Where(u => u.SuspendedUntil.HasValue
                        && u.SuspendedUntil.Value <= now
                        && u.SuspendedUntil.Value > now.AddMinutes(-2))
                    .ToListAsync();

                foreach (var user in recentlyUnbanned)
                {
                    _logger.LogInformation("用户 {UserId} 封禁已到期，发送解封通知", user.Id);

                    await notificationService.CreateNotificationAsync(
                        user.Id,
                        "预约权限已恢复",
                        "您的预约权限冻结期已结束，现在可以正常预约座位了。请珍惜预约机会，按时就座。",
                        NotificationType.BanExpired);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "封禁解封检查失败");
            }
        }

        /// <summary>处理候补确认超时</summary>
        private async Task HandleWaitlistTimeoutsAsync(AppDbContext dbContext, INotificationService notificationService, IServiceScope scope)
        {
            try
            {
                var now = DateTime.UtcNow;

                var expiredEntries = await dbContext.WaitlistEntries
                    .Where(w => w.Status == WaitlistStatus.Notified
                        && w.ConfirmDeadline.HasValue
                        && w.ConfirmDeadline.Value <= now)
                    .OrderBy(w => w.QueuePosition)
                    .ToListAsync();

                foreach (var entry in expiredEntries)
                {
                    entry.Status = WaitlistStatus.Expired;
                    _logger.LogInformation("候补确认超时 - WaitlistId: {Id}, 用户: {UserId}, 座位: {SeatNumber}",
                        entry.Id, entry.UserId, entry.SeatNumber);

                    await notificationService.CreateNotificationAsync(
                        entry.UserId,
                        "候补确认已超时",
                        $"您对座位 {entry.SeatNumber} 的候补确认已超时，名额已释放给下一位候补者。",
                        NotificationType.SystemMessage);

                    // 通知下一位候补者
                    var nextEntry = await dbContext.WaitlistEntries
                        .OrderBy(w => w.QueuePosition)
                        .FirstOrDefaultAsync(w =>
                            w.SeatNumber == entry.SeatNumber
                            && w.StartTime == entry.StartTime
                            && w.EndTime == entry.EndTime
                            && w.Status == WaitlistStatus.Waiting
                            && w.Id != entry.Id);

                    if (nextEntry != null)
                    {
                        var confirmDeadline = now.AddMinutes(15);
                        nextEntry.Status = WaitlistStatus.Notified;
                        nextEntry.NotifiedAt = now;
                        nextEntry.ConfirmDeadline = confirmDeadline;

                        await notificationService.CreateNotificationAsync(
                            nextEntry.UserId,
                            "候补名额可用",
                            $"座位 {nextEntry.SeatNumber} 现有名额可用，请在 15 分钟内确认预约，超时将顺延至下一位。",
                            NotificationType.WaitlistAvailable,
                            null);
                    }
                }

                if (expiredEntries.Count > 0)
                {
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "候补超时处理失败");
            }
        }
    }
}
