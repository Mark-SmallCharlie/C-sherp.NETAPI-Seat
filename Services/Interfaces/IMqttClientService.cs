using WebApplication1.Models.Mqtt;

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
