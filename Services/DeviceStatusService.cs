using WebApplication1.Models.Device;
using WebApplication1.Services.Interfaces;
using System.Collections.Concurrent;


namespace WebApplication1.Services
{
    public class DeviceStatusService : IDeviceStatusService
    {
        // 内存存储设备状态（对于小型应用足够，如果需要持久化可以改用数据库）
        private readonly ConcurrentDictionary<string, DeviceStatus> _deviceStatus = new();
        private readonly ConcurrentDictionary<string, DeviceSeatMapping> _deviceMappings = new();
        private readonly ILogger<DeviceStatusService> _logger;

        public DeviceStatusService(ILogger<DeviceStatusService> logger)
        {
            _logger = logger;
        }

        public Task UpdateDeviceStatusAsync(string deviceId, bool isOccupied, Dictionary<string, object>? additionalData = null)
        {
            var status = new DeviceStatus
            {
                DeviceId = deviceId,
                IsOccupied = isOccupied,
                LastUpdated = DateTime.UtcNow,
                AdditionalData = additionalData ?? new Dictionary<string, object>()
            };

            _deviceStatus.AddOrUpdate(deviceId, status, (key, oldValue) => status);

            _logger.LogDebug("设备状态更新 - Device: {DeviceId}, Occupied: {IsOccupied}", deviceId, isOccupied);

            return Task.CompletedTask;
        }

        public Task<DeviceStatus?> GetDeviceStatusAsync(string deviceId)
        {
            _deviceStatus.TryGetValue(deviceId, out var status);
            return Task.FromResult(status);
        }

        public Task<Dictionary<string, DeviceStatus>> GetAllDeviceStatusAsync()
        {
            return Task.FromResult(_deviceStatus.ToDictionary());
        }

        public Task<Dictionary<int, bool>> GetSeatOccupancyStatusAsync()
        {
            var result = new Dictionary<int, bool>();

            foreach (var mapping in _deviceMappings.Values)
            {
                if (_deviceStatus.TryGetValue(mapping.DeviceId, out var status))
                {
                    result[mapping.SeatNumber] = status.IsOccupied;
                }
            }

            return Task.FromResult(result);
        }

        public Task<bool> SetDeviceSeatMappingAsync(string deviceId, int seatNumber, string location)
        {
            var mapping = new DeviceSeatMapping
            {
                DeviceId = deviceId,
                SeatNumber = seatNumber,
                Location = location
            };

            _deviceMappings.AddOrUpdate(deviceId, mapping, (key, oldValue) => mapping);
            _logger.LogInformation("设备映射设置成功 - Device: {DeviceId}, Seat: {SeatNumber}", deviceId, seatNumber);

            return Task.FromResult(true);
        }

        public Task<bool> RemoveDeviceSeatMappingAsync(string deviceId)
        {
            var removed = _deviceMappings.TryRemove(deviceId, out _);
            if (removed)
            {
                _logger.LogInformation("设备映射移除成功 - Device: {DeviceId}", deviceId);
            }
            return Task.FromResult(removed);
        }
    }
}
