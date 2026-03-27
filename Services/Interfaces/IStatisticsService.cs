using WebApplication1.API.Services;
using WebApplication1.Models.DTOs.Responses;


namespace WebApplication1.Services.Interfaces;

public interface IStatisticsService
{

    Task<StatisticsResponse> GetDailyStatisticsAsync(DateTime date);
    Task<StatisticsResponse> GetMonthlyStatisticsAsync(int year, int month);
    Task<SeatUtilizationResponse> GetSeatUtilizationAsync();
    Task<PopularSeatResponse> GetPopularSeatsAsync(int topN = 10);
    Task<UserActivityResponse> GetUserActivityAsync(int days = 30);
}

public class StatisticsResponse
{
    public int TotalReservations { get; set; }
    public int ActiveReservations { get; set; }
    public int NewUsers { get; set; }
    public int PendingUsers { get; set; }
}

public class SeatUtilizationResponse
{
    public Dictionary<int, double> UtilizationRates { get; set; } = new(); // 座位号 -> 利用率
    public double OverallUtilization { get; set; }
}

