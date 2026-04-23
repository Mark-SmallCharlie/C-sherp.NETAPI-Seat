// 这是一个用于创建预订请求的DTO类，包含座位号、开始时间、结束时间以及用户的认证信息（用户名、密码和显示名称）。
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
