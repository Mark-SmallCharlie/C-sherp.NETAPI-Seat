using WebApplication1.Models.Mqtt;
// 这是一个用于定义MQTT客户端服务接口的代码文件。
namespace WebApplication1.Services.Interfaces
{
    public interface IMqttClientService
    {
        Task<bool> ConnectAsync();
        Task DisconnectAsync();
        Task SubscribeAsync(string topic);
        Task PublishAsync(string topic, string payload);
        bool IsConnected { get; }
    }
}
