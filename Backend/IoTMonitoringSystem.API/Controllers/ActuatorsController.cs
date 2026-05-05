using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace IoTMonitoringSystem.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ActuatorsController : ControllerBase
    {
        private readonly IActuatorService _actuatorService;

        public ActuatorsController(IActuatorService actuatorService)
        {
            _actuatorService = actuatorService;
        }

        // GET: api/v1/actuators/devices/{deviceId}/actuators
        [HttpGet("devices/{deviceId:int}/actuators")]
        public async Task<ActionResult<ApiResponse<List<ActuatorDto>>>> GetByDevice(int deviceId)
        {
            try
            {
                var items = await _actuatorService.GetByDeviceIdAsync(deviceId);
                return Ok(new ApiResponse<List<ActuatorDto>>
                {
                    Success = true,
                    Message = "Actuators retrieved successfully",
                    Data = items
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<List<ActuatorDto>>
                {
                    Success = false,
                    Message = "Device not found",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<ActuatorDto>>
                {
                    Success = false,
                    Message = "An error occurred",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/v1/actuators/devices/{deviceId}/actuators/{actuatorId}
        [HttpGet("devices/{deviceId:int}/actuators/{actuatorId:int}")]
        public async Task<ActionResult<ApiResponse<ActuatorDto>>> GetOne(int deviceId, int actuatorId)
        {
            try
            {
                var item = await _actuatorService.GetByIdAsync(deviceId, actuatorId);
                return Ok(new ApiResponse<ActuatorDto>
                {
                    Success = true,
                    Message = "Actuator retrieved successfully",
                    Data = item
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<ActuatorDto>
                {
                    Success = false,
                    Message = "Not found",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<ActuatorDto>
                {
                    Success = false,
                    Message = "An error occurred",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // POST: api/v1/actuators/devices/{deviceId}/actuators
        [HttpPost("devices/{deviceId:int}/actuators")]
        public async Task<ActionResult<ApiResponse<ActuatorDto>>> Create(int deviceId, [FromBody] CreateActuatorDto dto)
        {
            try
            {
                var created = await _actuatorService.CreateAsync(deviceId, dto);
                return CreatedAtAction(
                    nameof(GetOne),
                    new { deviceId, actuatorId = created.ActuatorId },
                    new ApiResponse<ActuatorDto>
                    {
                        Success = true,
                        Message = "Actuator created successfully",
                        Data = created
                    });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<ActuatorDto>
                {
                    Success = false,
                    Message = "Device not found",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<ActuatorDto>
                {
                    Success = false,
                    Message = "Invalid request",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<ActuatorDto>
                {
                    Success = false,
                    Message = "An error occurred",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // PUT: api/v1/actuators/devices/{deviceId}/actuators/{actuatorId}
        [HttpPut("devices/{deviceId:int}/actuators/{actuatorId:int}")]
        public async Task<ActionResult<ApiResponse<ActuatorDto>>> Update(
            int deviceId,
            int actuatorId,
            [FromBody] UpdateActuatorDto dto)
        {
            try
            {
                var updated = await _actuatorService.UpdateAsync(deviceId, actuatorId, dto);
                return Ok(new ApiResponse<ActuatorDto>
                {
                    Success = true,
                    Message = "Actuator updated successfully",
                    Data = updated
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<ActuatorDto>
                {
                    Success = false,
                    Message = "Not found",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<ActuatorDto>
                {
                    Success = false,
                    Message = "Invalid request",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<ActuatorDto>
                {
                    Success = false,
                    Message = "An error occurred",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // DELETE: api/v1/actuators/devices/{deviceId}/actuators/{actuatorId}
        [HttpDelete("devices/{deviceId:int}/actuators/{actuatorId:int}")]
        public async Task<ActionResult> Delete(int deviceId, int actuatorId)
        {
            try
            {
                await _actuatorService.DeleteAsync(deviceId, actuatorId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Not found",
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
    }
}
