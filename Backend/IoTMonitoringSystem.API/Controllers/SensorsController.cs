using Microsoft.AspNetCore.Mvc;
using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Services;

namespace IoTMonitoringSystem.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class SensorsController : ControllerBase
    {
        private readonly ISensorService _sensorService;

        public SensorsController(ISensorService sensorService)
        {
            _sensorService = sensorService;
        }

        // GET: api/v1/devices/{deviceId}/sensors
        [HttpGet("devices/{deviceId}/sensors")]
        public async Task<ActionResult<ApiResponse<List<SensorDto>>>> GetSensorsByDevice(int deviceId)
        {
            try
            {
                var sensors = await _sensorService.GetSensorsByDeviceIdAsync(deviceId);
                return Ok(new ApiResponse<List<SensorDto>>
                {
                    Success = true,
                    Message = "Sensors retrieved successfully",
                    Data = sensors
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<SensorDto>>
                {
                    Success = false,
                    Message = "An error occurred",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/v1/sensors/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<SensorDto>>> GetSensor(int id)
        {
            try
            {
                var sensor = await _sensorService.GetSensorByIdAsync(id);
                return Ok(new ApiResponse<SensorDto>
                {
                    Success = true,
                    Message = "Sensor retrieved successfully",
                    Data = sensor
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<SensorDto>
                {
                    Success = false,
                    Message = "Sensor not found",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<SensorDto>
                {
                    Success = false,
                    Message = "An error occurred",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // POST: api/v1/devices/{deviceId}/sensors
        [HttpPost("devices/{deviceId}/sensors")]
        public async Task<ActionResult<ApiResponse<SensorDto>>> CreateSensor(int deviceId, [FromBody] CreateSensorDto dto)
        {
            try
            {
                var sensor = await _sensorService.CreateSensorAsync(deviceId, dto);
                return CreatedAtAction(nameof(GetSensor), new { id = sensor.SensorId }, new ApiResponse<SensorDto>
                {
                    Success = true,
                    Message = "Sensor created successfully",
                    Data = sensor
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<SensorDto>
                {
                    Success = false,
                    Message = "Device not found",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<SensorDto>
                {
                    Success = false,
                    Message = "Failed to create sensor",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // PUT: api/v1/sensors/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<SensorDto>>> UpdateSensor(int id, [FromBody] UpdateSensorDto dto)
        {
            try
            {
                var sensor = await _sensorService.UpdateSensorAsync(id, dto);
                return Ok(new ApiResponse<SensorDto>
                {
                    Success = true,
                    Message = "Sensor updated successfully",
                    Data = sensor
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<SensorDto>
                {
                    Success = false,
                    Message = "Sensor not found",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<SensorDto>
                {
                    Success = false,
                    Message = "Failed to update sensor",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // DELETE: api/v1/sensors/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteSensor(int id)
        {
            try
            {
                await _sensorService.DeleteSensorAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Sensor not found",
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

