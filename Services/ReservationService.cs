using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebApplication1.Data;
using WebApplication1.Models.DTOs.Requests;
using WebApplication1.Models.Entities;
using WebApplication1.Services.Interfaces;

namespace WebApplication1.API.Services;

// 这是一个用于处理预约相关业务逻辑的服务类。
/*** ReservationService - 实现了 IReservationService 接口，
 * 提供预约相关的业务逻辑处理，包括创建预约、取消预约、获取用户预约列表、获取所有预约列表、检查座位冲突等功能。
 * 使用 Entity Framework Core 进行数据库操作，并通过依赖注入获取 AppDbContext 和 ILogger。
 * 包含输入验证、错误处理和日志记录，以确保系统的健壮性和可维护性。
 */
public class ReservationService : IReservationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ReservationService> _logger;
    private readonly IDeviceStatusService _deviceStatusService;

    public ReservationService(AppDbContext context, ILogger<ReservationService> logger, IDeviceStatusService deviceStatusService)
    {
        _context = context;
        _logger = logger;
        _deviceStatusService = deviceStatusService;
    }

    public async Task<Reservation?> CreateReservationAsync(CreateReservationRequest request, int userId)
    {
        try
        {
            _logger.LogInformation("创建预约 - 用户: {UserId}, 座位: {SeatNumber}, 时间: {StartTime} 到 {EndTime}",
                userId, request.SeatNumber, request.StartTime, request.EndTime);

            // 输入验证
            if (request.StartTime >= request.EndTime)
            {
                _logger.LogWarning("预约时间无效 - 开始时间不能在结束时间之后");
                return null;
            }

            // 【修改后】允许最多 5 分钟的误差
            if (request.StartTime < DateTime.Now.AddMinutes(-5))
            {
                _logger.LogWarning("预约时间无效 - 不能预约过去的时间 (请求时间: {StartTime}, 服务器当前时间: {Now})",
                    request.StartTime, DateTime.Now);
                return null;
            }

            if (request.SeatNumber <= 0 || request.SeatNumber > 1000) // 假设座位号范围
            {
                _logger.LogWarning("座位号无效: {SeatNumber}", request.SeatNumber);
                return null;
            }

            // 检查时间冲突
            var hasConflict = await CheckSeatConflictAsync(request.SeatNumber, request.StartTime, request.EndTime);
            if (hasConflict)
            {
                _logger.LogWarning("座位冲突 - 座位: {SeatNumber}, 时间: {StartTime} 到 {EndTime}",
                    request.SeatNumber, request.StartTime, request.EndTime);
                return null;
            }

            var reservation = new Reservation
            {
                UserId = userId,
                SeatNumber = request.SeatNumber,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Status = ReservationStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            // 记录座位状态变化
            await RecordSeatStatusChangeAsync(request.SeatNumber, true);

            _logger.LogInformation("预约创建成功 - ID: {ReservationId}", reservation.Id);
            return reservation;
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "数据库保存预约失败");
            throw new InvalidOperationException("创建预约时发生数据库错误", dbEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建预约失败");
            throw;
        }
    }

    public async Task<bool> CancelReservationAsync(int reservationId, int userId, bool isAdmin = false, string? adminNote = null)
    {
        try
        {
            _logger.LogInformation("取消预约 - ReservationId: {ReservationId}, UserId: {UserId}, IsAdmin: {IsAdmin}",
                reservationId, userId, isAdmin);

            var reservation = await _context.Reservations
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null)
            {
                _logger.LogWarning("预约不存在 - ReservationId: {ReservationId}", reservationId);
                return false;
            }

            // 检查权限：要么是预约的创建者，要么是管理员
            if (reservation.UserId != userId && !isAdmin)
            {
                _logger.LogWarning("权限不足 - 用户 {UserId} 无权取消预约 {ReservationId}", userId, reservationId);
                return false;
            }

            // 检查预约状态
            if (reservation.Status != ReservationStatus.Active)
            {
                _logger.LogWarning("预约状态不允许取消 - 当前状态: {Status}", reservation.Status);
                return false;
            }

            reservation.Status = isAdmin ? ReservationStatus.ForceCancelled : ReservationStatus.Cancelled;
            reservation.AdminNote = adminNote;

            await _context.SaveChangesAsync();

            // 记录座位状态变化
            await RecordSeatStatusChangeAsync(reservation.SeatNumber, false);

            _logger.LogInformation("预约取消成功 - ReservationId: {ReservationId}", reservationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消预约失败 - ReservationId: {ReservationId}", reservationId);
            throw;
        }
    }

    public async Task<List<Reservation>> GetUserReservationsAsync(int userId)
    {
        try
        {
            _logger.LogInformation("获取用户预约列表 - UserId: {UserId}", userId);

            var reservations = await _context.Reservations
                .Include(r => r.User)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // 自动更新过期预约状态
            await UpdateExpiredReservationsAsync(reservations);

            _logger.LogInformation("找到 {Count} 个用户预约", reservations.Count);
            return reservations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户预约列表失败 - UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<Reservation>> GetAllReservationsAsync()
    {
        try
        {
            _logger.LogInformation("获取所有预约列表");

            var reservations = await _context.Reservations
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // 自动更新过期预约状态
            await UpdateExpiredReservationsAsync(reservations);

            _logger.LogInformation("总预约数: {Count}", reservations.Count);
            return reservations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有预约列表失败");
            throw;
        }
    }

    public async Task<bool> CheckSeatConflictAsync(int seatNumber, DateTime startTime, DateTime endTime, int? excludeReservationId = null)
    {
        try
        {
            var query = _context.Reservations
                .Where(r => r.SeatNumber == seatNumber &&
                           (r.Status == ReservationStatus.Active || r.Status == ReservationStatus.Completed));

            if (excludeReservationId.HasValue)
            {
                query = query.Where(r => r.Id != excludeReservationId.Value);
            }

            var conflicts = await query
                .Where(r =>
                    (startTime >= r.StartTime && startTime < r.EndTime) ||
                    (endTime > r.StartTime && endTime <= r.EndTime) ||
                    (startTime <= r.StartTime && endTime >= r.EndTime)
                )
                .AnyAsync();

            if (conflicts)
            {
                _logger.LogDebug("座位冲突检测 - 座位: {SeatNumber}, 时间: {StartTime} 到 {EndTime}",
                    seatNumber, startTime, endTime);
            }

            return conflicts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查座位冲突失败");
            throw;
        }
    }

    public async Task<List<Reservation>> GetActiveReservationsAsync()
    {
        try
        {
            var activeReservations = await _context.Reservations
                .Include(r => r.User)
                .Where(r => r.Status == ReservationStatus.Active && r.EndTime > DateTime.Now)
                .OrderBy(r => r.StartTime)
                .ToListAsync();

            return activeReservations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活跃预约失败");
            throw;
        }
    }

    private async Task UpdateExpiredReservationsAsync(List<Reservation> reservations)
    {
        try
        {
            var now = DateTime.Now;
            var expiredReservations = reservations
                .Where(r => r.Status == ReservationStatus.Active && r.EndTime <= now)
                .ToList();

            if (expiredReservations.Any())
            {
                foreach (var reservation in expiredReservations)
                {
                    reservation.Status = ReservationStatus.Completed;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("自动更新 {Count} 个过期预约状态", expiredReservations.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新过期预约状态失败");
            // 不抛出异常，避免影响主流程
        }
    }

    private async Task RecordSeatStatusChangeAsync(int seatNumber, bool isOccupied)
    {
        try
        {
            var history = new SeatStatusHistory
            {
                SeatNumber = seatNumber,
                IsOccupied = isOccupied,
                Timestamp = DateTime.UtcNow
            };

            _context.SeatStatusHistories.Add(history);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录座位状态变化失败");
            // 不抛出异常，避免影响主流程
        }
    }

    public async Task<int> ReleaseTimeoutReservationsAsync()
    {
        try
        {
            var now = DateTime.Now;
            // 找出过了 30 分钟且仍在 Active 状态的预约
            var timeoutLimit = now.AddMinutes(-30);
            var potentialTimeoutReservations = await _context.Reservations
                .Where(r => r.Status == ReservationStatus.Active && r.StartTime <= timeoutLimit)
                .ToListAsync();

            if (!potentialTimeoutReservations.Any())
                return 0;

            var currentOccupancy = await _deviceStatusService.GetSeatOccupancyStatusAsync();
            int releasedCount = 0;

            foreach (var reservation in potentialTimeoutReservations)
            {
                // 检查设备状态中是否有人 (true=occupied, false=unoccupied/not-found=unoccupied)
                bool isOccupied = false;
                if (currentOccupancy != null && currentOccupancy.TryGetValue(reservation.SeatNumber, out var occupied))
                {
                    isOccupied = occupied;
                }

                if (!isOccupied)
                {
                    _logger.LogInformation("预约 {ReservationId} (座位 {SeatNumber}) 超时未落座，强制释放", reservation.Id, reservation.SeatNumber);
                    
                    reservation.Status = ReservationStatus.ForceCancelled;
                    reservation.AdminNote = "超时30分钟未落座自动释放";
                    releasedCount++;
                    
                    // 可选：同样记录到座位状态表中，或者如果已经被释放就没必要记了
                    await RecordSeatStatusChangeAsync(reservation.SeatNumber, false);
                }
            }

            if (releasedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return releasedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查和释放超时预约失败");
            return 0;
        }
    }
}
