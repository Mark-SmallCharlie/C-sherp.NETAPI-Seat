namespace WebApplication1.Models.DTOs.Responses;

/// <summary>
/// 日/月统计数据响应
/// </summary>
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

/// <summary>
/// 座位利用率统计响应（含预约利用率和硬件实际使用率）
/// </summary>
public class SeatUtilizationResponse
{
    public Dictionary<int, double> UtilizationRates { get; set; } = new();       // 预约利用率（基于Reservations表）
    public double OverallUtilization { get; set; }
    public int TotalSeats { get; set; }
    public int AnalyzedDays { get; set; }
    public int TotalReservations { get; set; }
    public Dictionary<int, double> ActualUtilizationRates { get; set; } = new(); // 实际使用率（基于SeatStatusHistory硬件数据）
    public double OverallActualUtilization { get; set; }
}

/// <summary>
/// 热门座位统计响应
/// </summary>
public class PopularSeatResponse
{
    public List<PopularSeat> PopularSeats { get; set; } = new();
    public DateTime AnalysisDate { get; set; }
}

/// <summary>
/// 热门座位条目
/// </summary>
public class PopularSeat
{
    public int SeatNumber { get; set; }
    public int ReservationCount { get; set; }
    public double TotalHours { get; set; }
}

/// <summary>
/// 用户活跃度统计响应
/// </summary>
public class UserActivityResponse
{
    public List<UserActivity> UserActivities { get; set; } = new();
    public int PeriodDays { get; set; }
    public int TotalActiveUsers { get; set; }
}

/// <summary>
/// 用户活跃度条目
/// </summary>
public class UserActivity
{
    public int UserId { get; set; }
    public int ReservationCount { get; set; }
    public double TotalHours { get; set; }
    public DateTime LastActivity { get; set; }
}
