using WebApplication1.Models.DTOs.Requests;
using WebApplication1.Models.Entities;
using WebApplication1.Models.DTOs.Responses;
using System.Reflection.Metadata.Ecma335;



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
