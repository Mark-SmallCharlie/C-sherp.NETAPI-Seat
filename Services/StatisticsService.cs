using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models.Entities;
using WebApplication1.Services.Interfaces;
using WebApplication1.Models.DTOs.Responses;
using System.Reflection.Metadata.Ecma335;


namespace WebApplication1.Services;

public class StatisticsService : IStatisticsService
{
    private readonly AppDbContext _context;
    private readonly ILogger<StatisticsService> _logger;

    public StatisticsService(AppDbContext context, ILogger<StatisticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<StatisticsResponse> GetDailyStatisticsAsync(DateTime date)
    {
        try
        {
            _logger.LogInformation("获取每日统计 - 日期: {Date:yyyy-MM-dd}", date);

            var startOfDay = date.Date;
            var endOfDay = date.Date.AddDays(1).AddTicks(-1);

            var totalReservations = await _context.Reservations
                .Where(r => r.CreatedAt >= startOfDay && r.CreatedAt <= endOfDay)
                .CountAsync();

            var activeReservations = await _context.Reservations
                .Where(r => r.Status == ReservationStatus.Active &&
                           r.StartTime >= startOfDay && r.StartTime <= endOfDay)
                .CountAsync();

            var newUsers = await _context.Users
                .Where(u => u.CreatedAt >= startOfDay && u.CreatedAt <= endOfDay)
                .CountAsync();

            var pendingUsers = await _context.Users
                .Where(u => u.Role == UserRole.Pending)
                .CountAsync();

            var statistics = new StatisticsResponse
            {
                TotalReservations = totalReservations,
                ActiveReservations = activeReservations,
                NewUsers = newUsers,
                PendingUsers = pendingUsers,
                Date = date
            };

            _logger.LogInformation("每日统计完成 - 预约: {TotalReservations}, 活跃: {ActiveReservations}, 新用户: {NewUsers}",
                totalReservations, activeReservations, newUsers);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取每日统计失败 - 日期: {Date:yyyy-MM-dd}", date);
            throw;
        }
        
    }

    public async Task<StatisticsResponse> GetMonthlyStatisticsAsync(int year, int month)
    {
        try
        {
            _logger.LogInformation("获取月度统计 - {Year}年{Month}月", year, month);

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddTicks(-1);

            var totalReservations = await _context.Reservations
                .Where(r => r.CreatedAt >= startDate && r.CreatedAt <= endDate)
                .CountAsync();

            var activeReservations = await _context.Reservations
                .Where(r => r.Status == ReservationStatus.Active &&
                           r.StartTime >= startDate && r.StartTime <= endDate)
                .CountAsync();

            var newUsers = await _context.Users
                .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                .CountAsync();

            var pendingUsers = await _context.Users
                .Where(u => u.Role == UserRole.Pending)
                .CountAsync();

            var statistics = new StatisticsResponse
            {
                TotalReservations = totalReservations,
                ActiveReservations = activeReservations,
                NewUsers = newUsers,
                PendingUsers = pendingUsers,
                Year = year,
                Month = month
            };

            _logger.LogInformation("月度统计完成 - 预约: {TotalReservations}, 活跃: {ActiveReservations}, 新用户: {NewUsers}",
                totalReservations, activeReservations, newUsers);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取月度统计失败 - {Year}年{Month}月", year, month);
            throw;
        }
    }

    public async Task<SeatUtilizationResponse> GetSeatUtilizationAsync()
    {
        try
        {
            _logger.LogInformation("获取座位利用率统计");

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30).Date;
            var totalSeats = 100; // 假设总共有100个座位

            var seatUtilizations = new Dictionary<int, double>();
            double totalUtilization = 0;

            // 获取最近30天的预约数据
            var recentReservations = await _context.Reservations
                .Where(r => r.StartTime >= thirtyDaysAgo &&
                           (r.Status == ReservationStatus.Active || r.Status == ReservationStatus.Completed))
                .GroupBy(r => r.SeatNumber)
                .Select(g => new
                {
                    SeatNumber = g.Key,
                    TotalHours = g.Sum(r => (r.EndTime - r.StartTime).TotalHours),
                    ReservationCount = g.Count()
                })
                .ToListAsync();

            var totalDays = (DateTime.UtcNow - thirtyDaysAgo).TotalDays;
            var totalAvailableHours = totalSeats * totalDays * 24; // 总可用小时数

            foreach (var seat in recentReservations)
            {
                var utilization = Math.Min(seat.TotalHours / (totalDays * 24), 1.0);
                seatUtilizations[seat.SeatNumber] = Math.Round(utilization * 100, 2); // 转换为百分比
            }

            // 计算总体利用率
            var totalUsedHours = recentReservations.Sum(r => r.TotalHours);
            var overallUtilization = totalAvailableHours > 0 ?
                Math.Round((totalUsedHours / totalAvailableHours) * 100, 2) : 0;

            var response = new SeatUtilizationResponse
            {
                UtilizationRates = seatUtilizations,
                OverallUtilization = overallUtilization,
                TotalSeats = totalSeats,
                AnalyzedDays = (int)totalDays,
                TotalReservations = recentReservations.Sum(r => r.ReservationCount)
            };

            _logger.LogInformation("座位利用率统计完成 - 总体利用率: {OverallUtilization}%", overallUtilization);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取座位利用率统计失败");
            throw;
        }
    }

    public async Task<PopularSeatResponse> GetPopularSeatsAsync(int topN = 10)
    {
        try
        {
            _logger.LogInformation("获取热门座位统计 - 前 {TopN} 名", topN);

            var popularSeats = await _context.Reservations
                .Where(r => r.Status == ReservationStatus.Active || r.Status == ReservationStatus.Completed)
                .GroupBy(r => r.SeatNumber)
                .Select(g => new PopularSeat
                {
                    SeatNumber = g.Key,
                    ReservationCount = g.Count(),
                    TotalHours = g.Sum(r => (r.EndTime - r.StartTime).TotalHours)
                })
                .OrderByDescending(s => s.ReservationCount)
                .Take(topN)
                .ToListAsync();

            var response = new PopularSeatResponse
            {
                PopularSeats = popularSeats,
                AnalysisDate = DateTime.UtcNow
            };

            _logger.LogInformation("热门座位统计完成");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取热门座位统计失败");
            throw;
        }
    }

    public async Task<UserActivityResponse> GetUserActivityAsync(int days = 30)
    {
        try
        {
            _logger.LogInformation("获取用户活跃度统计 - 最近 {Days} 天", days);

            var startDate = DateTime.UtcNow.AddDays(-days).Date;

            var userActivity = await _context.Reservations
                .Where(r => r.CreatedAt >= startDate)
                .GroupBy(r => r.UserId)
                .Select(g => new UserActivity
                {
                    UserId = g.Key,
                    ReservationCount = g.Count(),
                    TotalHours = g.Sum(r => (r.EndTime - r.StartTime).TotalHours),
                    LastActivity = g.Max(r => r.CreatedAt)
                })
                .OrderByDescending(u => u.ReservationCount)
                .ToListAsync();

            var response = new UserActivityResponse
            {
                UserActivities = userActivity,
                PeriodDays = days,
                TotalActiveUsers = userActivity.Count
            };

            _logger.LogInformation("用户活跃度统计完成 - 活跃用户数: {ActiveUsers}", userActivity.Count);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户活跃度统计失败");
            throw;
        }
    }

    Task<Interfaces.StatisticsResponse> IStatisticsService.GetDailyStatisticsAsync(DateTime date)
    {
        throw new NotImplementedException();
    }

    Task<Interfaces.StatisticsResponse> IStatisticsService.GetMonthlyStatisticsAsync(int year, int month)
    {
        throw new NotImplementedException();
    }

    Task<Interfaces.SeatUtilizationResponse> IStatisticsService.GetSeatUtilizationAsync()
    {
        throw new NotImplementedException();
    }
}

// 扩展的统计响应类
public class StatisticsResponse
{
    public int TotalReservations { get; set; }
    public int ActiveReservations { get; set; }
    public int NewUsers { get; set; }
    public int PendingUsers { get; set; }
    public DateTime? Date { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
}

public class SeatUtilizationResponse
{
    public Dictionary<int, double> UtilizationRates { get; set; } = new();
    public double OverallUtilization { get; set; }
    public int TotalSeats { get; set; }
    public int AnalyzedDays { get; set; }
    public int TotalReservations { get; set; }

    
}

public class PopularSeatResponse
{
    public List<PopularSeat> PopularSeats { get; set; } = new();
    public DateTime AnalysisDate { get; set; }
}

public class PopularSeat
{
    public int SeatNumber { get; set; }
    public int ReservationCount { get; set; }
    public double TotalHours { get; set; }
}

public class UserActivityResponse
{
    public List<UserActivity> UserActivities { get; set; } = new();
    public int PeriodDays { get; set; }
    public int TotalActiveUsers { get; set; }
}

public class UserActivity
{
    public int UserId { get; set; }
    public int ReservationCount { get; set; }
    public double TotalHours { get; set; }
    public DateTime LastActivity { get; set; }
}
