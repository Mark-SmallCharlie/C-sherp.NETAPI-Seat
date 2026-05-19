using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebApplication1.Services.Interfaces;
using System.Net.Http;


/** OneNet HTTP 轮询服务 - 实现了 BackgroundService 接口，
 * 定期通过 HTTP 请求 OneNet OpenAPI 获取设备的最新属性值，并更新座位状态。
 */
namespace WebApplication1.Services.OneNet
{
    public class OneNetPollingService : BackgroundService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OneNetPollingService> _logger;
        // 直接注入单例的设备状态服务
        private readonly IDeviceStatusService _deviceStatusService;

        public OneNetPollingService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<OneNetPollingService> logger,
            IDeviceStatusService deviceStatusService)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _deviceStatusService = deviceStatusService;
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
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);

                    // 请求 OneNet OpenAPI：查询设备的最新属性值
                    var url = $"https://iot-api.heclouds.com/thingmodel/query-device-property?product_id={pid}&device_name={deviceName}";

                    var response = await client.GetAsync(url, stoppingToken);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync(stoppingToken);
                        _logger.LogInformation("OneNet查询成功，返回数据: {Json}", json);

                        // 解析响应并更新座位状态
                        await ProcessDeviceDataAsync(json);
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

                // 每隔 3 秒轮询一次，加速前端响应
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }

        // ================== 核心修改：完美适配你的 JSON 格式 ==================
        private async Task ProcessDeviceDataAsync(string jsonResponse)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;

                // OneNet HTTP API 返回的标准格式 {"code": 0, "msg": "succ", "data": [ ... ] }
                if (root.TryGetProperty("code", out var code) && code.GetInt32() == 0)
                {
                    if (root.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                    {
                        // 遍历 data 数组里的每一个传感器 (seat_1, seat_2...)
                        foreach (var item in dataArray.EnumerateArray())
                        {
                            var identifier = item.GetProperty("identifier").GetString() ?? "";
                            string valueStr = "false";

                            if (item.TryGetProperty("value", out var valueElement))
                            {
                                valueStr = valueElement.ValueKind == JsonValueKind.String ? valueElement.GetString() ?? "false" : valueElement.GetRawText();
                            }

                            // 检查标识符是否是 "seat_1", "seat_2" 这种格式
                            if (identifier.StartsWith("seat_") && int.TryParse(identifier.Substring(5), out int seatNumber))
                            {
                                // 把字符串 "true"/"false" 转成布尔值
                                bool isOccupied = valueStr.ToLower() == "true";

                                // 1. 动态建立映射：告诉系统标识符 "seat_1" 对应前端的座位号 1
                                await _deviceStatusService.SetDeviceSeatMappingAsync(identifier, seatNumber, "阅览区");

                                // 2. 更新该座位的占用状态
                                await _deviceStatusService.UpdateDeviceStatusAsync(identifier, isOccupied);

                                _logger.LogInformation("状态同步成功 - 标识符: {Id}, 座位号: {Seat}, 座位状态: {Occ}",
                                    identifier, seatNumber, isOccupied);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析 HTTP 响应 JSON 失败，原始数据：{Json}", jsonResponse);
            }
        }

        private string GenerateHttpToken(string productId, string accessKey)
        {
            // 沿用你原来成功调通的固定鉴权签名
            return "version=2018-10-31&res=products%2FvCRg326c00%2Fdevices%2FESP8266&et=1806061800&method=md5&sign=UyNq6269lCxQEdc9EuWAZA%3D%3D";
        }
    }
}
