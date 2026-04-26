using System.ComponentModel.DataAnnotations;


/**
 * SeatStatusHistory实体类用于记录座位状态的变化历史。它包含了Id、SeatNumber、IsOccupied和Timestamp等属性。
 * 这些属性分别表示记录的唯一标识、座位编号、是否被占用以及状态变更的时间戳。
 * 通过这个类，开发者可以方便地在数据库中存储和查询座位状态的历史记录，以便进行分析和统计。
 */
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
