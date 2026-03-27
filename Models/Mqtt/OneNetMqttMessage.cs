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
