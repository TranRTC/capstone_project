using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace IoTMonitoringSystem.API.Controllers
{
    [ApiController]
    [Route("api/v1/devices/{deviceId:int}/configurations")]
    public class DeviceConfigurationsController : ControllerBase
    {
        private readonly IDeviceConfigurationService _deviceConfigurationService;

        public DeviceConfigurationsController(IDeviceConfigurationService deviceConfigurationService)
        {
            _deviceConfigurationService = deviceConfigurationService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<DeviceConfigurationDto>>>> GetByDeviceId(int deviceId)
        {
            try
            {
                var data = await _deviceConfigurationService.GetByDeviceIdAsync(deviceId);
                return Ok(new ApiResponse<List<DeviceConfigurationDto>>
                {
                    Success = true,
                    Message = "Device configurations retrieved successfully",
                    Data = data
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<List<DeviceConfigurationDto>>
                {
                    Success = false,
                    Message = "Device not found",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<DeviceConfigurationDto>>
                {
                    Success = false,
                    Message = "An error occurred",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPut]
        public async Task<ActionResult<ApiResponse<List<DeviceConfigurationDto>>>> UpsertByDeviceId(
            int deviceId,
            [FromBody] UpsertDeviceConfigurationsDto dto)
        {
            try
            {
                var data = await _deviceConfigurationService.UpsertByDeviceIdAsync(deviceId, dto);
                return Ok(new ApiResponse<List<DeviceConfigurationDto>>
                {
                    Success = true,
                    Message = "Device configurations updated successfully",
                    Data = data
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<List<DeviceConfigurationDto>>
                {
                    Success = false,
                    Message = "Device not found",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<List<DeviceConfigurationDto>>
                {
                    Success = false,
                    Message = "Failed to update device configurations",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }
}
