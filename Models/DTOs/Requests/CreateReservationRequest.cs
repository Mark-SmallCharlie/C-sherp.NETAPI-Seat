namespace WebApplication1.Models.DTOs.Requests
{
    public class CreateReservationRequest
    {
        public int SeatNumber { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}
