using WebApplication1.Models.Entities;

namespace WebApplication1.Services.Interfaces;

/// <summary>
/// 候补服务接口 — 加入候补、确认候补、取消候补、取消预约时推进队列。
/// </summary>
public interface IWaitlistService
{
    /// <summary>加入候补队列</summary>
    Task<WaitlistEntry?> JoinWaitlistAsync(int userId, int seatNumber, DateTime startTime, DateTime endTime);

    /// <summary>用户确认候补名额</summary>
    Task<WaitlistEntry?> ConfirmWaitlistAsync(int waitlistId, int userId);

    /// <summary>用户主动取消候补</summary>
    Task<bool> CancelWaitlistAsync(int waitlistId, int userId);

    /// <summary>获取用户在某个座位时段的候补状态</summary>
    Task<WaitlistEntry?> GetUserWaitlistAsync(int userId, int seatNumber, DateTime startTime, DateTime endTime);

    /// <summary>座位取消时推进候补队列：通知队列第一人</summary>
    Task PromoteWaitlistAsync(int seatNumber, DateTime startTime, DateTime endTime);
}
