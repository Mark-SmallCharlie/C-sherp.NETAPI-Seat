/**
 * OneNetMqttMessage类用于表示从OneNet平台接收到的MQTT消息。
 * 它包含了设备ID和数据流列表等属性。
 */

namespace WebApplication1.Models.Mqtt
{
    public class OneNetMqttMessage

    {
        public string DeviceId { get; set; } = "vCRg326c00";
        public List<DataStreamMessage> Data { get; set; } = new();
    }

    public class DataStreamMessage
    {
        public string Id { get; set; } = string.Empty;
        public object Value { get; set; } = new();
        public DateTime At { get; set; }
    }
}
