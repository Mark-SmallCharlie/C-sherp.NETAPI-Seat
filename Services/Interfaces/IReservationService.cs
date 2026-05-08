using WebApplication1.Models.DTOs.Requests;
using WebApplication1.Models.Entities;
using WebApplication1.Models.DTOs.Responses;
using System.Reflection.Metadata.Ecma335;


// 这是一个用于定义预约服务接口的代码文件。
// IReservationService接口包含了创建预约、取消预约、获取用户预约、获取所有预约、获取活跃预约以及检查座位冲突等方法。
namespace WebApplication1.Services.Interfaces;

public interface IReservationService
{
    Task<Reservation?> CreateReservationAsync(CreateReservationRequest request, int userId);
   
    Task<bool> CancelReservationAsync(int reservationId, int userId, bool isAdmin = false, string? adminNote = null);
    Task<List<Reservation>> GetUserReservationsAsync(int userId);
    Task<List<Reservation>> GetAllReservationsAsync();
    Task<List<Reservation>> GetActiveReservationsAsync();
    Task<bool> CheckSeatConflictAsync(int seatNumber, DateTime startTime, DateTime endTime, int? excludeReservationId = null);

    /// <summary>
    /// 设置暂离状态，延缓自动释放
    /// </summary>
    Task<bool> SetTemporaryLeaveAsync(int reservationId, int userId, int minutes = 15);

    /// <summary>
    /// 取消暂离状态
    /// </summary>
    Task<bool> ReturnFromLeaveAsync(int reservationId, int userId);

    /// <summary>
    /// 定期检查超时的预约并自动强制取消
    /// </summary>
    /// <returns>被影响的预约数量</returns>
    Task<int> ReleaseTimeoutReservationsAsync();
}
