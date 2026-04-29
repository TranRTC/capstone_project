using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace IoTMonitoringSystem.API.Controllers
{
    [ApiController]
    [Route("api/v1/devices/{deviceId:int}/commands")]
    public class DeviceCommandsController : ControllerBase
    {
        private readonly IDeviceCommandService _deviceCommandService;

        public DeviceCommandsController(IDeviceCommandService deviceCommandService)
        {
            _deviceCommandService = deviceCommandService;
        }

        // POST: api/v1/devices/{deviceId}/commands
        [HttpPost]
        public async Task<ActionResult<ApiResponse<DeviceCommandDto>>> CreateCommand(
            int deviceId,
            [FromBody] CreateDeviceCommandDto dto)
        {
            try
            {
                var requestedBy = User?.Identity?.Name;
                var command = await _deviceCommandService.CreateCommandAsync(deviceId, dto, requestedBy);

                return CreatedAtAction(nameof(GetCommandById), new { commandId = command.CommandId }, new ApiResponse<DeviceCommandDto>
                {
                    Success = true,
                    Message = "Device command created successfully",
                    Data = command
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<DeviceCommandDto>
                {
                    Success = false,
                    Message = "Device not found",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<DeviceCommandDto>
                {
                    Success = false,
                    Message = "Invalid command payload",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<DeviceCommandDto>
                {
                    Success = false,
                    Message = "Command not supported for this device",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<DeviceCommandDto>
                {
                    Success = false,
                    Message = "An error occurred",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/v1/devices/{deviceId}/commands
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<DeviceCommandDto>>>> GetCommandsByDevice(
            int deviceId,
            [FromQuery] DeviceCommandQueryDto query)
        {
            try
            {
                var result = await _deviceCommandService.GetCommandsByDeviceIdAsync(deviceId, query);

                return Ok(new ApiResponse<PagedResult<DeviceCommandDto>>
                {
                    Success = true,
                    Message = "Device commands retrieved successfully",
                    Data = result
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<PagedResult<DeviceCommandDto>>
                {
                    Success = false,
                    Message = "Device not found",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<PagedResult<DeviceCommandDto>>
                {
                    Success = false,
                    Message = "An error occurred",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/v1/commands/{commandId}
        [HttpGet("~/api/v1/commands/{commandId:long}")]
        public async Task<ActionResult<ApiResponse<DeviceCommandDto>>> GetCommandById(long commandId)
        {
            try
            {
                var result = await _deviceCommandService.GetCommandByIdAsync(commandId);

                return Ok(new ApiResponse<DeviceCommandDto>
                {
                    Success = true,
                    Message = "Device command retrieved successfully",
                    Data = result
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<DeviceCommandDto>
                {
                    Success = false,
                    Message = "Command not found",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<DeviceCommandDto>
                {
                    Success = false,
                    Message = "An error occurred",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }
}
