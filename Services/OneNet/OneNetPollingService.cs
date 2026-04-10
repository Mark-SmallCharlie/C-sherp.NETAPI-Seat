using System.Security.Cryptography;
using System.Text.Json;
using WebApplication1.Services.Interfaces;

namespace WebApplication1.Services.OneNet
{
    public class OneNetPollingService : BackgroundService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OneNetPollingService> _logger;

        public OneNetPollingService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            ILogger<OneNetPollingService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OneNet HTTP 轮询服务已启动...");
            
            // 延迟一点，等程序主体启动完成
            await Task.Delay(3000, stoppingToken);

            var pid = _configuration["OneNet:ProductId"];
            var deviceName = _configuration["OneNet:DeviceName"];
            var accessKey = _configuration["OneNet:AccessKey"];

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var client = _httpClientFactory.CreateClient("OneNet");
                    
                    // 生成 HTTP 调用专属的鉴权 Token
                    var token = GenerateHttpToken(pid, accessKey);
                    
                    // OneNet API 要求的 HTTP Header
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("Authorization", token);

                    // 请求 OneNet OpenAPI：查询设备的最新属性值
                    var url = $"https://iot-api.heclouds.com/thingmodel/query-device-property?product_id={pid}&device_name={deviceName}";

                    var response = await client.GetAsync(url, stoppingToken);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync(stoppingToken);
                        
                        // 解析响应并更新座位状态
                        await ProcessDeviceDataAsync(json, deviceName);
                    }
                    else
                    {
                        var errInfo = await response.Content.ReadAsStringAsync(stoppingToken);
                        _logger.LogWarning("拉取设备数据失败，状态码: {Code}, 信息: {Msg}", response.StatusCode, errInfo);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "HTTP 轮询 OneNet 失败");
                }

                // 每隔 5 秒轮询一次，可根据需要调整时间，防止触发云平台流控
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        private async Task ProcessDeviceDataAsync(string jsonResponse, string deviceId)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;
                
                // OneNet HTTP API 返回的标准格式通常是 {"code": 0, "msg": "succ", "data": [ ... ] }
                if (root.TryGetProperty("code", out var code) && code.GetInt32() == 0)
                {
                    if (root.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                    {
                        bool? isOccupied = null;
                        var additionalData = new Dictionary<string, object>();

                        foreach (var item in dataArray.EnumerateArray())
                        {
                            var identifier = item.GetProperty("identifier").GetString();
                            var value = item.GetProperty("value").GetString(); 
                            
                            if (identifier != null && identifier.ToLower() == "occupancy") // 根据你的雷达物模型标识符调整
                            {
                                isOccupied = value == "1" || value?.ToLower() == "true";
                            }
                            else if(identifier != null)
                            {
                                additionalData[identifier] = value ?? "";
                            }
                        }

                        if (isOccupied.HasValue)
                        {
                            using var scope = _serviceProvider.CreateScope();
                            var deviceStatusService = scope.ServiceProvider.GetRequiredService<IDeviceStatusService>();
                            await deviceStatusService.UpdateDeviceStatusAsync(deviceId, isOccupied.Value, additionalData);
                            
                            _logger.LogInformation("HTTP同步设备最新状态 - DeviceId: {Device}, IsOccupied: {Occ}", deviceId, isOccupied.Value);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "解析 HTTP 响应 JSON 失败，原始数据：{Json}", jsonResponse);
            }
        }

        private string GenerateHttpToken(string productId, string accessKey)
        {
            var res = $"products/{productId}";
            var et = DateTimeOffset.UtcNow.AddYears(1).ToUnixTimeSeconds().ToString();
            var method = "sha1";
            var version = "2018-10-31";

            var signStr = $"{et}\n{method}\n{res}\n{version}";
            using var hmac = new HMACSHA1(System.Text.Encoding.UTF8.GetBytes(accessKey));
            var sign = Convert.ToBase64String(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signStr)));

            // URL 编码
            return $"version={version}&res={Uri.EscapeDataString(res)}&et={et}&method={method}&sign={Uri.EscapeDataString(sign)}";
        }
    }
}