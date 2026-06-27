using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IoTMonitoringSystem.API.Services;
using IoTMonitoringSystem.Core.DTOs;

namespace IoTMonitoringSystem.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AgentController : ControllerBase
    {
        private readonly IAgentService _agentService;
        private readonly IProactiveAgentService _proactiveAgentService;
        private readonly ILogger<AgentController> _logger;

        public AgentController(
            IAgentService agentService,
            IProactiveAgentService proactiveAgentService,
            ILogger<AgentController> logger)
        {
            _agentService = agentService;
            _proactiveAgentService = proactiveAgentService;
            _logger = logger;
        }

        [HttpGet("status")]
        public ActionResult<ApiResponse<AgentStatusDto>> GetStatus()
        {
            var status = _agentService.GetStatus();
            return Ok(new ApiResponse<AgentStatusDto>
            {
                Success = true,
                Message = status.Configured ? "Assistant is ready." : "Assistant is not configured.",
                Data = status
            });
        }

        [HttpGet("proactive/status")]
        public async Task<ActionResult<ApiResponse<AgentProactiveStatusDto>>> GetProactiveStatus(
            CancellationToken cancellationToken)
        {
            try
            {
                var status = await _proactiveAgentService.GetProactiveStatusAsync(cancellationToken);
                return Ok(new ApiResponse<AgentProactiveStatusDto>
                {
                    Success = true,
                    Message = status.Enabled ? "Proactive monitoring is enabled." : "Proactive monitoring is disabled.",
                    Data = status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load proactive agent status");
                return StatusCode(503, new ApiResponse<AgentProactiveStatusDto>
                {
                    Success = false,
                    Message = "Proactive monitoring is unavailable. Restart the API to apply database migrations.",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("insights")]
        public async Task<ActionResult<ApiResponse<PagedResult<AgentInsightDto>>>> GetInsights(
            [FromQuery] AgentInsightQueryDto query,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _proactiveAgentService.GetInsightsAsync(query, cancellationToken);
                return Ok(new ApiResponse<PagedResult<AgentInsightDto>>
                {
                    Success = true,
                    Message = "Insights retrieved successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load agent insights");
                return StatusCode(500, new ApiResponse<PagedResult<AgentInsightDto>>
                {
                    Success = false,
                    Message = "Could not load insights. Restart the API to apply the AgentInsights database migration.",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("insights/{id:long}/dismiss")]
        public async Task<ActionResult<ApiResponse<AgentInsightDto>>> DismissInsight(
            long id,
            CancellationToken cancellationToken)
        {
            try
            {
                var insight = await _proactiveAgentService.DismissInsightAsync(id, cancellationToken);
                return Ok(new ApiResponse<AgentInsightDto>
                {
                    Success = true,
                    Message = "Insight dismissed.",
                    Data = insight
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<AgentInsightDto>
                {
                    Success = false,
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("insights/{id:long}/open-in-chat")]
        public async Task<ActionResult<ApiResponse<AgentOpenInChatResponse>>> OpenInChat(
            long id,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _proactiveAgentService.GetOpenInChatSeedAsync(id, cancellationToken);
                return Ok(new ApiResponse<AgentOpenInChatResponse>
                {
                    Success = true,
                    Message = "Chat seed generated.",
                    Data = result
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<AgentOpenInChatResponse>
                {
                    Success = false,
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("chat")]
        public async Task<ActionResult<ApiResponse<AgentChatResponse>>> Chat(
            [FromBody] AgentChatRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new ApiResponse<AgentChatResponse>
                {
                    Success = false,
                    Message = "Message is required.",
                    Errors = new List<string> { "Message cannot be empty." }
                });
            }

            try
            {
                var result = await _agentService.ChatAsync(request, User, cancellationToken);
                return Ok(new ApiResponse<AgentChatResponse>
                {
                    Success = true,
                    Message = "Assistant reply generated.",
                    Data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Agent chat unavailable");
                return StatusCode(503, new ApiResponse<AgentChatResponse>
                {
                    Success = false,
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent chat failed");
                return StatusCode(500, new ApiResponse<AgentChatResponse>
                {
                    Success = false,
                    Message = "The assistant could not process your request.",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }
}
