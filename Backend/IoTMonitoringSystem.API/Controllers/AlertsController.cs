using Microsoft.AspNetCore.Mvc;
using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Services;

namespace IoTMonitoringSystem.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AlertsController : ControllerBase
    {
        private readonly IAlertService _alertService;

        public AlertsController(IAlertService alertService)
        {
            _alertService = alertService;
        }

        // GET: api/v1/alerts
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<AlertDto>>>> GetAlerts([FromQuery] AlertQueryDto query)
        {
            try
            {
                var result = await _alertService.GetAlertsAsync(query);
                return Ok(new ApiResponse<PagedResult<AlertDto>>
                {
                    Success = true,
                    Message = "Alerts retrieved successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<PagedResult<AlertDto>>
                {
                    Success = false,
                    Message = "An error occurred",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/v1/alerts/active
        [HttpGet("active")]
        public async Task<ActionResult<ApiResponse<List<AlertDto>>>> GetActiveAlerts()
        {
            try
            {
                var alerts = await _alertService.GetActiveAlertsAsync();
                return Ok(new ApiResponse<List<AlertDto>>
                {
                    Success = true,
                    Message = "Active alerts retrieved successfully",
                    Data = alerts
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<AlertDto>>
                {
                    Success = false,
                    Message = "An error occurred",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/v1/alerts/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<AlertDto>>> GetAlert(long id)
        {
            try
            {
                var alert = await _alertService.GetAlertByIdAsync(id);
                return Ok(new ApiResponse<AlertDto>
                {
                    Success = true,
                    Message = "Alert retrieved successfully",
                    Data = alert
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<AlertDto>
                {
                    Success = false,
                    Message = "Alert not found",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<AlertDto>
                {
                    Success = false,
                    Message = "An error occurred",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // PUT: api/v1/alerts/{id}/acknowledge
        [HttpPut("{id}/acknowledge")]
        public async Task<ActionResult<ApiResponse<AlertDto>>> AcknowledgeAlert(long id)
        {
            try
            {
                var alert = await _alertService.AcknowledgeAlertAsync(id);
                return Ok(new ApiResponse<AlertDto>
                {
                    Success = true,
                    Message = "Alert acknowledged successfully",
                    Data = alert
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<AlertDto>
                {
                    Success = false,
                    Message = "Alert not found",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<AlertDto>
                {
                    Success = false,
                    Message = "Failed to acknowledge alert",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // PUT: api/v1/alerts/{id}/resolve
        [HttpPut("{id}/resolve")]
        public async Task<ActionResult<ApiResponse<AlertDto>>> ResolveAlert(long id)
        {
            try
            {
                var alert = await _alertService.ResolveAlertAsync(id);
                return Ok(new ApiResponse<AlertDto>
                {
                    Success = true,
                    Message = "Alert resolved successfully",
                    Data = alert
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<AlertDto>
                {
                    Success = false,
                    Message = "Alert not found",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<AlertDto>
                {
                    Success = false,
                    Message = "Failed to resolve alert",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }
}

