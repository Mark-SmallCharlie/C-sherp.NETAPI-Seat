using WebApplication1.API.Services;
using WebApplication1.Models.DTOs.Responses;
using WebApplication1.Services;


namespace WebApplication1.Services.Interfaces;

// 这是一个用于定义统计服务接口的代码文件。
// IStatisticsService接口包含了获取每日统计数据、每月统计数据、座位利用率、热门座位以及用户活动等方法。
public interface IStatisticsService
{
    Task<StatisticsResponse> GetDailyStatisticsAsync(DateTime date);
    Task<StatisticsResponse> GetMonthlyStatisticsAsync(int year, int month);
    Task<SeatUtilizationResponse> GetSeatUtilizationAsync();
    Task<PopularSeatResponse> GetPopularSeatsAsync(int topN = 10);
    Task<UserActivityResponse> GetUserActivityAsync(int days = 30);
}

// 以下两个类已在 WebApplication1.Services 命名空间中统一定义（StatisticsService.cs 末尾），
// 此处注释掉避免类型冲突导致显式接口实现抛 NotImplementedException。
// public class StatisticsResponse
// {
//     public int TotalReservations { get; set; }
//     public int ActiveReservations { get; set; }
//     public int NewUsers { get; set; }
//     public int PendingUsers { get; set; }
// }
//
// public class SeatUtilizationResponse
// {
//     public Dictionary<int, double> UtilizationRates { get; set; } = new(); // 座位号 -> 利用率
//     public double OverallUtilization { get; set; }
// }

