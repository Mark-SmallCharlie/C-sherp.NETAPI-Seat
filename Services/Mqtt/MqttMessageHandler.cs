using WebApplication1.Models.Device;
using WebApplication1.Models.Mqtt;
using WebApplication1.Services.Interfaces;
using System.Text.Json;


namespace WebApplication1.Services.Mqtt
{
    public class MqttMessageHandler : IMqttMessageHandler
    {
        private readonly IDeviceStatusService _deviceStatusService;
        private readonly ILogger<MqttMessageHandler> _logger;

        public MqttMessageHandler(IDeviceStatusService deviceStatusService, ILogger<MqttMessageHandler> logger)
        {
            _deviceStatusService = deviceStatusService;
            _logger = logger;
        }

        public async Task HandleMessageAsync(string topic, string payload)
        {
            try
            {
                _logger.LogDebug("处理MQTT消息 - Topic: {Topic}", topic);

                var message = JsonSerializer.Deserialize<OneNetMqttMessage>(payload);
                if (message == null)
                {
                    _logger.LogWarning("MQTT消息格式无效: {Payload}", payload);
                    return;
                }

                if (topic.Contains("/dp"))
                {
                    await HandleDataPointMessage(message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理MQTT消息异常 - Topic: {Topic}, Payload: {Payload}", topic, payload);
            }
        }

        private async Task HandleDataPointMessage(OneNetMqttMessage message)
        {
            var deviceId = message.DeviceId;
            var additionalData = new Dictionary<string, object>();
            bool? isOccupied = null;

            if (message.Data != null)
            {
                foreach (var dataPoint in message.Data)
                {
                    var dataStreamId = dataPoint.Id.ToLower();
                    var value = dataPoint.Value;

                    _logger.LogDebug("处理数据点 - Device: {vCRg326c00}, Stream: {DataStreamId}, Value: {Value}",
                        deviceId, dataStreamId, value);

                    if (dataStreamId == "occupancy")
                    {
                        isOccupied = ConvertToBoolean(value);
                    }
                    else
                    {
                        additionalData[dataStreamId] = value;
                    }
                }
            }

            if (isOccupied.HasValue)
            {
                await _deviceStatusService.UpdateDeviceStatusAsync(deviceId, isOccupied.Value, additionalData);

                _logger.LogInformation("设备状态同步完成 - Device: {DeviceId}, Occupied: {IsOccupied}",
                    deviceId, isOccupied.Value);
            }
        }

        private bool ConvertToBoolean(object value)
        {
            if (value == null) return false;

            return value.ToString()?.ToLower() switch
            {
                "1" or "true" or "on" => true,
                "0" or "false" or "off" => false,
                _ => false
            };
        }
    }
}
