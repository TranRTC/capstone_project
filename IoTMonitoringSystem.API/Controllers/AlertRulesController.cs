using Microsoft.AspNetCore.Mvc;
using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Services;

namespace IoTMonitoringSystem.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AlertRulesController : ControllerBase
    {
        private readonly IAlertRuleService _alertRuleService;

        public AlertRulesController(IAlertRuleService alertRuleService)
        {
            _alertRuleService = alertRuleService;
        }

        // GET: api/v1/alertrules
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<AlertRuleDto>>>> GetAlertRules()
        {
            try
            {
                var rules = await _alertRuleService.GetAllAlertRulesAsync();
                return Ok(new ApiResponse<List<AlertRuleDto>>
                {
                    Success = true,
                    Message = "Alert rules retrieved successfully",
                    Data = rules
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<AlertRuleDto>>
                {
                    Success = false,
                    Message = "An error occurred",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/v1/alertrules/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<AlertRuleDto>>> GetAlertRule(int id)
        {
            try
            {
                var rule = await _alertRuleService.GetAlertRuleByIdAsync(id);
                return Ok(new ApiResponse<AlertRuleDto>
                {
                    Success = true,
                    Message = "Alert rule retrieved successfully",
                    Data = rule
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<AlertRuleDto>
                {
                    Success = false,
                    Message = "Alert rule not found",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<AlertRuleDto>
                {
                    Success = false,
                    Message = "An error occurred",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // GET: api/v1/alertrules/devices/{deviceId}
        [HttpGet("devices/{deviceId}")]
        public async Task<ActionResult<ApiResponse<List<AlertRuleDto>>>> GetAlertRulesByDevice(int deviceId)
        {
            try
            {
                var rules = await _alertRuleService.GetAlertRulesByDeviceIdAsync(deviceId);
                return Ok(new ApiResponse<List<AlertRuleDto>>
                {
                    Success = true,
                    Message = "Alert rules retrieved successfully",
                    Data = rules
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<AlertRuleDto>>
                {
                    Success = false,
                    Message = "An error occurred",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // POST: api/v1/alertrules
        [HttpPost]
        public async Task<ActionResult<ApiResponse<AlertRuleDto>>> CreateAlertRule([FromBody] CreateAlertRuleDto dto)
        {
            try
            {
                var rule = await _alertRuleService.CreateAlertRuleAsync(dto);
                return CreatedAtAction(nameof(GetAlertRule), new { id = rule.AlertRuleId }, new ApiResponse<AlertRuleDto>
                {
                    Success = true,
                    Message = "Alert rule created successfully",
                    Data = rule
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<AlertRuleDto>
                {
                    Success = false,
                    Message = "Device or sensor not found",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<AlertRuleDto>
                {
                    Success = false,
                    Message = "Failed to create alert rule",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // PUT: api/v1/alertrules/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<AlertRuleDto>>> UpdateAlertRule(int id, [FromBody] UpdateAlertRuleDto dto)
        {
            try
            {
                var rule = await _alertRuleService.UpdateAlertRuleAsync(id, dto);
                return Ok(new ApiResponse<AlertRuleDto>
                {
                    Success = true,
                    Message = "Alert rule updated successfully",
                    Data = rule
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<AlertRuleDto>
                {
                    Success = false,
                    Message = "Alert rule not found",
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<AlertRuleDto>
                {
                    Success = false,
                    Message = "Failed to update alert rule",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        // DELETE: api/v1/alertrules/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAlertRule(int id)
        {
            try
            {
                await _alertRuleService.DeleteAlertRuleAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Alert rule not found",
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

