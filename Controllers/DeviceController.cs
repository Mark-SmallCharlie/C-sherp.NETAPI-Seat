using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models.Device;
using WebApplication1.Controllers;
using WebApplication1.Services.Interfaces;

namespace WebApplication1.Controllers
{
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
                return Ok(new { success = true, data = status, message = "获取设备状态成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备状态异常");
                return StatusCode(500, new { success = false, message = "获取设备状态失败" });
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
                    return NotFound(new { success = false, message = "设备不存在或暂无数据" });
                }
                return Ok(new { success = true, data = status, message = "获取设备状态成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备状态异常 - Device: {DeviceId}", deviceId);
                return StatusCode(500, new { success = false, message = "获取设备状态失败" });
            }
        }

        [HttpGet("seat-occupancy")]
        public async Task<IActionResult> GetSeatOccupancyStatus()
        {
            try
            {
                var status = await _deviceStatusService.GetSeatOccupancyStatusAsync();
                return Ok(new { success = true, data = status, message = "获取座位占用状态成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取座位占用状态异常");
                return StatusCode(500, new { success = false, message = "获取座位占用状态失败" });
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
                    return BadRequest(new { success = false, message = "请求数据无效" });
                }

                await _deviceStatusService.SetDeviceSeatMappingAsync(
                    request.DeviceId, request.SeatNumber, request.Location);

                return Ok(new { success = true, message = "设置设备映射成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置设备映射异常 - Device: {DeviceId}", request.DeviceId);
                return StatusCode(500, new { success = false, message = "设置设备映射失败" });
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
                    return NotFound(new { success = false, message = "设备映射不存在" });
                }
                return Ok(new { success = true, message = "移除设备映射成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除设备映射异常 - Device: {DeviceId}", deviceId);
                return StatusCode(500, new { success = false, message = "移除设备映射失败" });
            }
        }

        [HttpGet("mappings")]
        public async Task<IActionResult> GetDeviceMappings()
        {
            try
            {
                // 返回一些基本信息
                return Ok(new { success = true, message = "映射管理功能运行正常" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备映射列表异常");
                return StatusCode(500, new { success = false, message = "获取设备映射列表失败" });
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
