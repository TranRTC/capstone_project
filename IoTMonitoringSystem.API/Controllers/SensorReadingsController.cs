using Microsoft.AspNetCore.Mvc;
using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Services;

namespace IoTMonitoringSystem.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class SensorReadingsController : ControllerBase
    {
        private readonly ISensorReadingService _readingService;

        public SensorReadingsController(ISensorReadingService readingService)
        {
            _readingService = readingService;
        }

        // POST: api/v1/sensorreadings
        [HttpPost]
        public async Task<ActionResult<ApiResponse<SensorReadingDto>>> CreateReading([FromBody] CreateSensorReadingDto dto)
        {
            try
            {
                var reading = await _readingService.CreateReadingAsync(dto);
                return CreatedAtAction(nameof(GetReading), new { id = reading.ReadingId }, new ApiResponse<SensorReadingDto>
                {
                    Success = true,
                    Message = "Sensor reading created successfully",
                    Data = reading
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<SensorReadingDto>
                {
                    Success = false,
                    Message = "Device or sensor not found",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<SensorReadingDto>
                {
                    Success = false,
                    Message = "Failed to create sensor reading",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // POST: api/v1/sensorreadings/batch
        [HttpPost("batch")]
        public async Task<ActionResult<ApiResponse<List<SensorReadingDto>>>> CreateReadingsBatch([FromBody] BatchCreateSensorReadingDto dto)
        {
            try
            {
                var readings = await _readingService.CreateReadingsBatchAsync(dto.Readings);
                return Ok(new ApiResponse<List<SensorReadingDto>>
                {
                    Success = true,
                    Message = $"Sensor readings created successfully. Count: {readings.Count}",
                    Data = readings
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<List<SensorReadingDto>>
                {
                    Success = false,
                    Message = "Failed to create sensor readings",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/v1/sensorreadings
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<SensorReadingDto>>>> GetReadings([FromQuery] SensorReadingQueryDto query)
        {
            try
            {
                var result = await _readingService.GetReadingsAsync(query);
                return Ok(new ApiResponse<PagedResult<SensorReadingDto>>
                {
                    Success = true,
                    Message = "Sensor readings retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<PagedResult<SensorReadingDto>>
                {
                    Success = false,
                    Message = "An error occurred",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/v1/sensorreadings/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<SensorReadingDto>>> GetReading(long id)
        {
            // This would require adding GetByIdAsync to the service
            // For now, return not implemented
            return StatusCode(501, new ApiResponse<SensorReadingDto>
            {
                Success = false,
                Message = "Not implemented"
            });
        }

        // GET: api/v1/devices/{deviceId}/readings
        [HttpGet("devices/{deviceId}/readings")]
        public async Task<ActionResult<ApiResponse<List<SensorReadingDto>>>> GetReadingsByDevice(
            int deviceId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                var readings = await _readingService.GetReadingsByDeviceIdAsync(deviceId, startDate, endDate);
                return Ok(new ApiResponse<List<SensorReadingDto>>
                {
                    Success = true,
                    Message = "Sensor readings retrieved successfully",
                    Data = readings
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<SensorReadingDto>>
                {
                    Success = false,
                    Message = "An error occurred",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/v1/sensors/{sensorId}/readings
        [HttpGet("sensors/{sensorId}/readings")]
        public async Task<ActionResult<ApiResponse<List<SensorReadingDto>>>> GetReadingsBySensor(
            int sensorId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                var readings = await _readingService.GetReadingsBySensorIdAsync(sensorId, startDate, endDate);
                return Ok(new ApiResponse<List<SensorReadingDto>>
                {
                    Success = true,
                    Message = "Sensor readings retrieved successfully",
                    Data = readings
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<SensorReadingDto>>
                {
                    Success = false,
                    Message = "An error occurred",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }
}

