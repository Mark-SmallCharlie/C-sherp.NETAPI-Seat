using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.Entities     //记录座位状态变化
{
    public class SeatStatusHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SeatNumber { get; set; } // 座位编号

        [Required]
        public bool IsOccupied { get; set; } // 是否被占用

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow; // 状态变更时间


    }
}
