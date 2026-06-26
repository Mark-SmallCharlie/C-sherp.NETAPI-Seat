using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Controllers;
using WebApplication1.Services.Interfaces;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// 设备控制器 — 处理设备状态查询、座位占用状态、设备-座位映射等请求。
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeviceController : BaseController
    {
        private readonly IDeviceStatusService _deviceStatusService;
        private readonly ILogger<DeviceController> _logger;

        public DeviceController(IDeviceStatusService deviceStatusService, ILogger<DeviceController> logger)
        {
            _deviceStatusService = deviceStatusService;
            _logger = logger;
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetAllDeviceStatus()
        {
            try
            {
                var status = await _deviceStatusService.GetAllDeviceStatusAsync();
                return OkResponse(status, "获取设备状态成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备状态异常");
                return ServerErrorResponse("获取设备状态失败");
            }
        }

        [HttpGet("status/{deviceId}")]
        public async Task<IActionResult> GetDeviceStatus(string deviceId)
        {
            try
            {
                var status = await _deviceStatusService.GetDeviceStatusAsync(deviceId);
                if (status == null)
                {
                    return NotFoundResponse("设备不存在或暂无数据");
                }
                return OkResponse(status, "获取设备状态成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备状态异常 - Device: {DeviceId}", deviceId);
                return ServerErrorResponse("获取设备状态失败");
            }
        }

        [HttpGet("seat-occupancy")]
        public async Task<IActionResult> GetSeatOccupancyStatus()
        {
            try
            {
                var status = await _deviceStatusService.GetSeatOccupancyStatusAsync();
                return OkResponse(status, "获取座位占用状态成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取座位占用状态异常");
                return ServerErrorResponse("获取座位占用状态失败");
            }
        }

        [HttpPost("mapping")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetDeviceMapping([FromBody] SetDeviceMappingRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequestResponse("请求数据无效");
                }

                await _deviceStatusService.SetDeviceSeatMappingAsync(
                    request.DeviceId, request.SeatNumber, request.Location);

                return OkResponse<object>(null, "设置设备映射成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置设备映射异常 - Device: {DeviceId}", request.DeviceId);
                return ServerErrorResponse("设置设备映射失败");
            }
        }

        [HttpDelete("mapping/{deviceId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveDeviceMapping(string deviceId)
        {
            try
            {
                var result = await _deviceStatusService.RemoveDeviceSeatMappingAsync(deviceId);
                if (!result)
                {
                    return NotFoundResponse("设备映射不存在");
                }
                return OkResponse<object>(null, "移除设备映射成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除设备映射异常 - Device: {DeviceId}", deviceId);
                return ServerErrorResponse("移除设备映射失败");
            }
        }

        [HttpGet("mappings")]
        public async Task<IActionResult> GetDeviceMappings()
        {
            try
            {
                return OkResponse<object>(null, "映射管理功能运行正常");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备映射列表异常");
                return ServerErrorResponse("获取设备映射列表失败");
            }
        }
    }

    public class SetDeviceMappingRequest
    {
        public string DeviceId { get; set; } = string.Empty;
        public int SeatNumber { get; set; }
        public string Location { get; set; } = string.Empty;
    }
}
