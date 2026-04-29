// 此接口定义了一个处理MQTT消息的异步方法，接受主题和负载作为参数。实现这个接口的类可以根据需要处理不同的MQTT消息，例如将消息存储到数据库、触发其他服务等。
namespace WebApplication1.Services.Interfaces
{
    public interface IMqttMessageHandler
    {
        Task HandleMessageAsync(string topic, string payload);
    }
}
