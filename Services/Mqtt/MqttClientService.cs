
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System;
//using MQTTNet.Client.Model;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WebApplication1.Models.Mqtt;
using WebApplication1.Services.Interfaces;
namespace WebApplication1.Services.Mqtt
{
    public class MqttClientService : IMqttClientService
    {
        private readonly IManagedMqttClient _mqttClient;
        private readonly MqttOptions _options;
        private readonly ILogger<MqttClientService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public bool IsConnected => _mqttClient.IsConnected;

        public MqttClientService(
            IOptions<MqttOptions> options,
            ILogger<MqttClientService> logger,
            IServiceProvider serviceProvider)
        {
            _options = options.Value;
            _logger = logger;
            _serviceProvider = serviceProvider;

            var factory = new MqttFactory();
            _mqttClient = factory.CreateManagedMqttClient();

            ConfigureEventHandlers();
        }

        private void ConfigureEventHandlers()
        {
            _mqttClient.ConnectedAsync += OnConnectedAsync;
            _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        }

        public async Task<bool> ConnectAsync()
        {
            var token = GenerateOneNetStudioToken(
                 _options.ProductId,      // 需要额外在 MqttOptions 中添加 ProductId 属性
                 _options.DeviceName,
                 _options.AccessKey);     // 需要添加 AccessKey 属性


            // 如果已经连接或客户端已启动，直接返回成功（避免重复启动）
            if (_mqttClient.IsConnected)
            {
                _logger.LogDebug("MQTT 客户端已连接，跳过");
                return true;
            }

            try
            {

                var clientOptions = new MqttClientOptionsBuilder()
                    //.WithTcpServer(_options.Server.Replace("mqtt://", ""), _options.Port)  //含协议头
                    .WithTcpServer(_options.Server, _options.Port)   // 直接使用 _options.Server（不含协议头）
                    .WithClientId(_options.ClientId)
                    .WithCredentials(_options.Username, _options.Password)
                    .WithCleanSession()
                    .Build();

                var managedOptions = new ManagedMqttClientOptionsBuilder()
                    .WithClientOptions(clientOptions)
                    .WithAutoReconnectDelay(TimeSpan.FromSeconds(_options.ReconnectDelaySeconds))
                    .Build();

                await _mqttClient.StartAsync(managedOptions);

                _logger.LogInformation("MQTT客户端启动成功");
                return true;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already started"))
            {
                // 捕获“已启动”异常，视为成功
                _logger.LogWarning("MQTT客户端已启动，忽略");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MQTT连接失败");
                return false;
            }
        }



        private string GenerateOneNetStudioToken(string productId, string deviceName, string accessKey)
        {
            var res = $"products/vCRg326c00/devices/ESP8266";
            var et = (DateTimeOffset.UtcNow.AddYears(10).ToUnixTimeSeconds()).ToString();
            var method = "sha1";
            var version = "2018-10-31";

            var signStr = $"{et}\n{method}\n{res}\n{version}";
            var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(accessKey));
            var sign = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(signStr)));

            return $"version={version}&res={res}&et={et}&method={method}&sign={sign}";
        }

        public async Task DisconnectAsync()
        {
            await _mqttClient.StopAsync();
            _logger.LogInformation("MQTT客户端已断开连接");
        }

        public async Task SubscribeAsync(string topic)
        {
            try
            {
                await _mqttClient.SubscribeAsync(topic);
                _logger.LogInformation("MQTT订阅成功: {Topic}", topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MQTT订阅失败: {Topic}", topic);
            }
        }

        public async Task PublishAsync(string topic, string payload)
        {
            try
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await _mqttClient.EnqueueAsync(message);
                _logger.LogDebug("MQTT消息发布成功: {Topic}", topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MQTT消息发布失败: {Topic}", topic);
            }
        }

        private Task OnConnectedAsync(EventArgs arg)
        {
            _logger.LogInformation("MQTT连接已建立");
            return Task.CompletedTask;
        }

        private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs arg)
        {
            _logger.LogWarning("MQTT连接断开: {Reason}", arg.Exception?.Message ?? "未知原因");
            return Task.CompletedTask;
        }

        private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            try
            {
                var topic = arg.ApplicationMessage.Topic;
                var payload = arg.ApplicationMessage.ConvertPayloadToString();

                _logger.LogInformation("收到MQTT消息 - Topic: {Topic}, Payload: {Payload}", topic, payload);

                using var scope = _serviceProvider.CreateScope();
                var messageHandler = scope.ServiceProvider.GetRequiredService<IMqttMessageHandler>();

                await messageHandler.HandleMessageAsync(topic, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理MQTT消息时发生异常");
            }
        }
    }
}
