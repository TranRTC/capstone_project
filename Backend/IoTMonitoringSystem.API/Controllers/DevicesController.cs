using Microsoft.AspNetCore.Mvc;
using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Services;

namespace IoTMonitoringSystem.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class DevicesController : ControllerBase
    {
        private readonly IDeviceService _deviceService;

        public DevicesController(IDeviceService deviceService)
        {
            _deviceService = deviceService;
        }

        // GET: api/v1/devices
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<DeviceListDto>>>> GetDevices()
        {
            try
            {
                var devices = await _deviceService.GetAllDevicesAsync();
                return Ok(new ApiResponse<List<DeviceListDto>>
                {
                    Success = true,
                    Message = "Devices retrieved successfully",
                    Data = devices
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<DeviceListDto>>
                {
                    Success = false,
                    Message = "An error occurred",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/v1/devices/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<DeviceDto>>> GetDevice(int id)
        {
            try
            {
                var device = await _deviceService.GetDeviceByIdAsync(id);
                return Ok(new ApiResponse<DeviceDto>
                {
                    Success = true,
                    Message = "Device retrieved successfully",
                    Data = device
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<DeviceDto>
                {
                    Success = false,
                    Message = "Device not found",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<DeviceDto>
                {
                    Success = false,
                    Message = "An error occurred",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // POST: api/v1/devices
        [HttpPost]
        public async Task<ActionResult<ApiResponse<DeviceDto>>> CreateDevice([FromBody] CreateDeviceDto dto)
        {
            // Model validation is automatic with [ApiController] attribute
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new ApiResponse<DeviceDto>
                {
                    Success = false,
                    Message = "Validation failed",
                    Errors = errors
                });
            }

            try
            {
                var device = await _deviceService.CreateDeviceAsync(dto);
                return CreatedAtAction(nameof(GetDevice), new { id = device.DeviceId }, new ApiResponse<DeviceDto>
                {
                    Success = true,
                    Message = "Device created successfully",
                    Data = device
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<DeviceDto>
                {
                    Success = false,
                    Message = "Failed to create device",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // PUT: api/v1/devices/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<DeviceDto>>> UpdateDevice(int id, [FromBody] UpdateDeviceDto dto)
        {
            try
            {
                var device = await _deviceService.UpdateDeviceAsync(id, dto);
                return Ok(new ApiResponse<DeviceDto>
                {
                    Success = true,
                    Message = "Device updated successfully",
                    Data = device
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<DeviceDto>
                {
                    Success = false,
                    Message = "Device not found",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<DeviceDto>
                {
                    Success = false,
                    Message = "Failed to update device",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // DELETE: api/v1/devices/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteDevice(int id)
        {
            try
            {
                await _deviceService.DeleteDeviceAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Device not found",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/v1/devices/{id}/status
        [HttpGet("{id}/status")]
        public async Task<ActionResult<ApiResponse<DeviceStatusDto>>> GetDeviceStatus(int id)
        {
            try
            {
                var device = await _deviceService.GetDeviceByIdAsync(id);
                var status = new DeviceStatusDto
                {
                    DeviceId = device.DeviceId,
                    Status = device.LastSeenAt.HasValue && device.LastSeenAt.Value > DateTime.UtcNow.AddMinutes(-5) ? "Online" : "Offline",
                    LastSeenAt = device.LastSeenAt
                };

                return Ok(new ApiResponse<DeviceStatusDto>
                {
                    Success = true,
                    Message = "Device status retrieved successfully",
                    Data = status
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<DeviceStatusDto>
                {
                    Success = false,
                    Message = "Device not found",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<DeviceStatusDto>
                {
                    Success = false,
                    Message = "An error occurred",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }
}

