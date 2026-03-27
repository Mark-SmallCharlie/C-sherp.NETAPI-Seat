using WebApplication1.Models.Device;
//using WebApplication1.Models.Device;

namespace WebApplication1.Services.Interfaces
{
    public interface IDeviceStatusService
    {
        // 更新设备状态
        Task UpdateDeviceStatusAsync(string deviceId, bool isOccupied, Dictionary<string, object>? additionalData = null);

        // 获取设备状态
        Task<DeviceStatus?> GetDeviceStatusAsync(string deviceId);

        // 获取所有设备状态
        Task<Dictionary<string, DeviceStatus>> GetAllDeviceStatusAsync();

        // 获取座位占用状态（通过设备映射）
        Task<Dictionary<int, bool>> GetSeatOccupancyStatusAsync();

        // 配置设备-座位映射
        Task<bool> SetDeviceSeatMappingAsync(string deviceId, int seatNumber, string location);

        // 移除设备-座位映射
        Task<bool> RemoveDeviceSeatMappingAsync(string deviceId);
    }
}
