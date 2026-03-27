namespace WebApplication1.Models.Mqtt
{
    public class MqttOptions
    {
        public string Server { get; set; } = "mqtt://mqtt.heclouds.com";
        public int Port { get; set; } = 1883;
        public string ClientId { get; set; } = "ESP8266";
        public string Username { get; set; } = "vCRg326c00"; // 通常是产品ID
        public string Password { get; set; } = "version=2018-10-31&res=products%2FvCRg326c00%2Fdevices%2FESP8266&et=1806061800&method=md5&sign=UyNq6269lCxQEdc9EuWAZA%3D%3D"; // 通常是产品API Key或设备密钥
        public int ReconnectDelaySeconds { get; set; } = 5;
        public string ProductId { get; set; } = "vCRg326c00";   // 产品ID
        public string DeviceName { get; set; } = "ESP8266";  // 设备名称
        public string AccessKey { get; set; } = "cGcwd3hNRWYyaFNHU0haSk5CN2QybnprR240SHFqQzc=";   // AccessKey（在控制台获取）

        public string[] SubscribeTopics { get; set; } = Array.Empty<string>();
    }
}
