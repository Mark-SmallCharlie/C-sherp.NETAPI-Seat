using WebApplication1.Services.Interfaces;


namespace WebApplication1.Services.Mqtt
{
    public class MqttBackgroundService : BackgroundService
    {
        private readonly IMqttClientService _mqttClientService;
        private readonly ILogger<MqttBackgroundService> _logger;
        private readonly IConfiguration _configuration;

        public MqttBackgroundService(
            IMqttClientService mqttClientService,
            ILogger<MqttBackgroundService> logger,
            IConfiguration configuration)
        {
            _mqttClientService = mqttClientService;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MQTT后台服务启动");

            // 等待应用完全启动
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);


            if (_mqttClientService.IsConnected)
            {
                _logger.LogInformation("MQTT连接成功，自动重连已启用");
                // 保持服务运行，等待停止信号
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            else
            {
                _logger.LogError("MQTT连接失败，服务将停止");
            }

            //while (!_mqttClientService.IsConnected)
            //{
                try
                {
                    if (!_mqttClientService.IsConnected)
                    {
                        _logger.LogInformation("尝试连接MQTT服务器...");

                        var connected = await _mqttClientService.ConnectAsync();
                        if (connected)
                        {
                        // 订阅所有设备的数据点主题
                        // OneNET主题格式: $sys/{productId}/{deviceName}/dp/post/json/+
                        //$sys/{ID}/{name}/thing/property/post 上报主题
                        //$sys/{ID}/{name}/thing/property/post/replay  上报回复主题
                        //$sys/{ID}/{name}/thing/property/set
                        var productId = _configuration["vCRg326c00"];
                            var topic = $"$sys/vCRg326c00/ESP8266/dp/post/json/+";

                            await _mqttClientService.SubscribeAsync(topic);
                            _logger.LogInformation("MQTT订阅设置完成: {Topic}", topic);
                        }
                        else
                        {
                            _logger.LogWarning("MQTT连接失败，5秒后重试...");
                            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                        }
                    }
                    else
                    {
                        // 保持连接，定期检查
                        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MQTT后台服务异常");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }

          //  }
            _logger.LogInformation("MQTT后台服务停止");


        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _mqttClientService.DisconnectAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
