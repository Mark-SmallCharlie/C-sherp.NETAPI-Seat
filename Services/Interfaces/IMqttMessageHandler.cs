namespace WebApplication1.Services.Interfaces
{
    public interface IMqttMessageHandler
    {
        Task HandleMessageAsync(string topic, string payload);
    }
}
