namespace WebApplication1.Models.DTOs.Requests;

/// <summary>
/// 座位冲突检测请求
/// </summary>
public class CheckConflictRequest
{
    public int SeatNumber { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int? ExcludeReservationId { get; set; }
}
