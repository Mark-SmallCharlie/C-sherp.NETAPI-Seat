using WebApplication1.Models.DTOs.Responses;

namespace WebApplication1.Services.Interfaces;

/// <summary>
/// 统计服务接口，包含每日/月度统计、座位利用率、热门座位、用户活跃度等方法。
/// 所有响应类型定义在 Models/DTOs/Responses/StatisticsResponses.cs。
/// </summary>
public interface IStatisticsService
{
    Task<StatisticsResponse> GetDailyStatisticsAsync(DateTime date);
    Task<StatisticsResponse> GetMonthlyStatisticsAsync(int year, int month);
    Task<SeatUtilizationResponse> GetSeatUtilizationAsync();
    Task<PopularSeatResponse> GetPopularSeatsAsync(int topN = 10);
    Task<UserActivityResponse> GetUserActivityAsync(int days = 30);
}
