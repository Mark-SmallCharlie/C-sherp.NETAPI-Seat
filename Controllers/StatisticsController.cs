using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using WebApplication1.Controllers;
using WebApplication1.Services.Interfaces;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class StatisticsController : BaseController
{
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<StatisticsController> _logger;

    public StatisticsController(IStatisticsService statisticsService, ILogger<StatisticsController> logger)
    {
        _statisticsService = statisticsService;
        _logger = logger;
    }

    [HttpGet("daily/{date}")]
    public async Task<IActionResult> GetDailyStatistics(DateTime date)
    {
        try
        {
            var statistics = await _statisticsService.GetDailyStatisticsAsync(date);

            _logger.LogInformation("获取每日统计 - 日期: {Date:yyyy-MM-dd}", date);
            return OkResponse(statistics, "获取每日统计成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取每日统计异常 - 日期: {Date:yyyy-MM-dd}", date);
            return ServerErrorResponse("获取每日统计失败");
        }
    }

    [HttpGet("monthly/{year}/{month}")]
    public async Task<IActionResult> GetMonthlyStatistics(int year, int month)
    {
        try
        {
            if (year < 2020 || year > 2100 || month < 1 || month > 12)
            {
                return BadRequestResponse("年份或月份无效");
            }

            var statistics = await _statisticsService.GetMonthlyStatisticsAsync(year, month);

            _logger.LogInformation("获取月度统计 - {Year}年{Month}月", year, month);
            return OkResponse(statistics, "获取月度统计成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取月度统计异常 - {Year}年{Month}月", year, month);
            return ServerErrorResponse("获取月度统计失败");
        }
    }

    [HttpGet("seat-utilization")]
    public async Task<IActionResult> GetSeatUtilization()
    {
        try
        {
            var utilization = await _statisticsService.GetSeatUtilizationAsync();

            _logger.LogInformation("获取座位利用率统计");
            return OkResponse(utilization, "获取座位利用率统计成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取座位利用率统计异常");
            return ServerErrorResponse("获取座位利用率统计失败");
        }
    }

    [HttpGet("popular-seats")]
    public async Task<IActionResult> GetPopularSeats([FromQuery] int topN = 10)
    {
        try
        {
            if (topN < 1 || topN > 100)
            {
                return BadRequestResponse("topN参数应在1-100之间");
            }

            var popularSeats = await _statisticsService.GetPopularSeatsAsync(topN);

            _logger.LogInformation("获取热门座位统计 - TopN: {TopN}", topN);
            return OkResponse(popularSeats, "获取热门座位统计成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取热门座位统计异常");
            return ServerErrorResponse("获取热门座位统计失败");
        }
    }

    [HttpGet("user-activity")]
    public async Task<IActionResult> GetUserActivity([FromQuery] int days = 30)
    {
        try
        {
            if (days < 1 || days > 365)
            {
                return BadRequestResponse("days参数应在1-365之间");
            }

            var userActivity = await _statisticsService.GetUserActivityAsync(days);

            _logger.LogInformation("获取用户活跃度统计 - 天数: {Days}", days);
            return OkResponse(userActivity, "获取用户活跃度统计成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户活跃度统计异常");
            return ServerErrorResponse("获取用户活跃度统计失败");
        }
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardData()
    {
        try
        {
            var today = DateTime.Today;
            var monthlyStats = await _statisticsService.GetMonthlyStatisticsAsync(today.Year, today.Month);
            var seatUtilization = await _statisticsService.GetSeatUtilizationAsync();
            var popularSeats = await _statisticsService.GetPopularSeatsAsync(5);
            var userActivity = await _statisticsService.GetUserActivityAsync(7);

            var dashboardData = new
            {
                MonthlyStatistics = monthlyStats,
                SeatUtilization = seatUtilization,
                TopSeats = popularSeats,
                WeeklyUserActivity = userActivity
            };

            _logger.LogInformation("获取仪表板数据");
            return OkResponse(dashboardData, "获取仪表板数据成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取仪表板数据异常");
            return ServerErrorResponse("获取仪表板数据失败");
        }
    }
}
