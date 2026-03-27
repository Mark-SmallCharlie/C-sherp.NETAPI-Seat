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
