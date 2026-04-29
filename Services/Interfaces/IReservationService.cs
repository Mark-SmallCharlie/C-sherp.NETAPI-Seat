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

}
