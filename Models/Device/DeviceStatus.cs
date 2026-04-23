// 这是一个设备状态模型，包含设备ID、座位映射、占用状态、最后更新时间以及其他可选数据。
// 还定义了一个设备与座位的映射配置类，可以用于存储设备与座位之间的关系。
namespace WebApplication1.Models.Device
{
    public class DeviceStatus
    {
        public string DeviceId { get; set; } = string.Empty;
        public int? SeatNumber { get; set; } // 可选的座位映射
        public bool IsOccupied { get; set; }
        public DateTime LastUpdated { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new(); // 温度、湿度等其他数据
    }

    // 设备与座位的映射配置（可以存储在数据库或配置文件中）
    public class DeviceSeatMapping
    {
        public string DeviceId { get; set; } = string.Empty;
        public int SeatNumber { get; set; }
        public string Location { get; set; } = string.Empty;
    }
}
