using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IoTMonitoringSystem.API.Services;
using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Infrastructure.Repositories;

namespace IoTMonitoringSystem.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AgentController : ControllerBase
    {
        private readonly IAgentService _agentService;
        private readonly IProactiveAgentService _proactiveAgentService;
        private readonly IAgentActionService _agentActionService;
        private readonly IAgentAuditRepository _auditRepository;
        private readonly IAgentChatSessionService _chatSessionService;
        private readonly ILogger<AgentController> _logger;

        public AgentController(
            IAgentService agentService,
            IProactiveAgentService proactiveAgentService,
            IAgentActionService agentActionService,
            IAgentAuditRepository auditRepository,
            IAgentChatSessionService chatSessionService,
            ILogger<AgentController> logger)
        {
            _agentService = agentService;
            _proactiveAgentService = proactiveAgentService;
            _agentActionService = agentActionService;
            _auditRepository = auditRepository;
            _chatSessionService = chatSessionService;
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

        [HttpGet("mcp/status")]
        public ActionResult<ApiResponse<AgentMcpStatusDto>> GetMcpStatus([FromServices] IConfiguration configuration)
        {
            var enabled = configuration.GetValue("Mcp:Enabled", true);
            var path = configuration["Mcp:HttpPath"] ?? "/mcp";
            var requireKey = configuration.GetValue("Mcp:RequireApiKey", true);
            var tools = new[]
            {
                "get_devices", "get_device", "get_active_alerts", "get_alerts",
                "get_sensors_by_device", "get_actuators_by_device", "get_recent_readings",
                "get_system_health", "search_documentation",
                "find_devices", "get_alert_summary", "get_operational_snapshot",
                "get_sensor_reading_summary"
            };

            return Ok(new ApiResponse<AgentMcpStatusDto>
            {
                Success = true,
                Message = enabled ? "MCP server is enabled." : "MCP server is disabled.",
                Data = new AgentMcpStatusDto
                {
                    Enabled = enabled,
                    HttpPath = path,
                    RequireApiKey = requireKey,
                    ToolNames = tools,
                    SetupHint = enabled
                        ? $"Connect MCP clients to http://localhost:5000{path} with header X-Mcp-Api-Key (see Documents/MCP.md)."
                        : "Set Mcp:Enabled to true in appsettings."
                }
            });
        }

        [HttpGet("suggested-prompts")]
        public ActionResult<ApiResponse<AgentSuggestedPromptsDto>> GetSuggestedPrompts(
            [FromQuery] int? deviceId,
            [FromServices] IConfiguration configuration)
        {
            var prompts = new List<string>
            {
                "Show operational snapshot",
                "Summarize active alerts",
                "Which devices are offline?",
                "Is MQTT healthy?"
            };

            if (deviceId.HasValue)
            {
                prompts.Insert(0, $"What's wrong with device {deviceId}?");
                prompts.Insert(1, $"Sensor reading summary for device {deviceId}");
                prompts.Insert(2, $"Show actuators on device {deviceId}");
            }

            return Ok(new ApiResponse<AgentSuggestedPromptsDto>
            {
                Success = true,
                Message = "Suggested prompts loaded.",
                Data = new AgentSuggestedPromptsDto { Prompts = prompts }
            });
        }

        [HttpGet("metrics")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<AgentMetricsDto>>> GetMetrics()
        {
            var since = DateTime.UtcNow.AddHours(-24);
            var metrics = await _auditRepository.GetMetricsAsync(since);
            return Ok(new ApiResponse<AgentMetricsDto>
            {
                Success = true,
                Message = "Assistant metrics (last 24h).",
                Data = metrics
            });
        }

        [HttpGet("audit")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<List<AgentAuditLogDto>>>> GetAuditLog([FromQuery] int take = 50)
        {
            take = Math.Clamp(take, 1, 200);
            var logs = await _auditRepository.GetRecentAsync(take);
            return Ok(new ApiResponse<List<AgentAuditLogDto>>
            {
                Success = true,
                Message = $"Last {logs.Count} audit entries.",
                Data = logs.Select(l => new AgentAuditLogDto
                {
                    AgentAuditLogId = l.AgentAuditLogId,
                    EventType = l.EventType,
                    Username = l.Username,
                    UserRole = l.UserRole,
                    ToolName = l.ToolName,
                    Summary = l.Summary,
                    RelatedDeviceId = l.RelatedDeviceId,
                    RelatedAlertId = l.RelatedAlertId,
                    DurationMs = l.DurationMs,
                    Success = l.Success,
                    CreatedAt = l.CreatedAt
                }).ToList()
            });
        }

        [HttpGet("sessions/{sessionId:long}")]
        public async Task<ActionResult<ApiResponse<AgentChatSessionDto>>> GetSession(long sessionId)
        {
            var username = User.Identity?.Name ?? User.FindFirst("unique_name")?.Value;
            if (string.IsNullOrWhiteSpace(username))
                return Unauthorized();

            var session = await _chatSessionService.GetSessionDtoAsync(sessionId, username);
            if (session is null)
                return NotFound(new ApiResponse<AgentChatSessionDto> { Success = false, Message = "Session not found." });

            return Ok(new ApiResponse<AgentChatSessionDto>
            {
                Success = true,
                Message = "Chat session loaded.",
                Data = session
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

        [HttpGet("actions/pending")]
        public async Task<ActionResult<ApiResponse<AgentActionProposalDto>>> GetPendingAction(
            CancellationToken cancellationToken)
        {
            try
            {
                var pending = await _agentActionService.GetPendingForUserAsync(User, cancellationToken);
                return Ok(new ApiResponse<AgentActionProposalDto>
                {
                    Success = true,
                    Message = pending is null ? "No pending actions." : "Pending action retrieved.",
                    Data = pending
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load pending agent action");
                return StatusCode(500, new ApiResponse<AgentActionProposalDto>
                {
                    Success = false,
                    Message = "Could not load pending action. Restart the API to apply the AgentActionProposals migration.",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("actions/{id:long}/confirm")]
        [Authorize(Roles = "Admin,Operator")]
        public async Task<ActionResult<ApiResponse<AgentActionResultDto>>> ConfirmAction(
            long id,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _agentActionService.ConfirmAsync(id, User, cancellationToken);
                return Ok(new ApiResponse<AgentActionResultDto>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<AgentActionResultDto>
                {
                    Success = false,
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<AgentActionResultDto>
                {
                    Success = false,
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to confirm agent action {Id}", id);
                return StatusCode(500, new ApiResponse<AgentActionResultDto>
                {
                    Success = false,
                    Message = "Could not execute the action.",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("actions/{id:long}/cancel")]
        public async Task<ActionResult<ApiResponse<AgentActionProposalDto>>> CancelAction(
            long id,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _agentActionService.CancelAsync(id, User, cancellationToken);
                return Ok(new ApiResponse<AgentActionProposalDto>
                {
                    Success = true,
                    Message = "Action cancelled.",
                    Data = result
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<AgentActionProposalDto>
                {
                    Success = false,
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<AgentActionProposalDto>
                {
                    Success = false,
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("reports/run-digest")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<AgentInsightDto>>> RunDigest(
            [FromQuery] string type = "Daily",
            CancellationToken cancellationToken = default)
        {
            try
            {
                AgentInsightDto? insight = type.Equals("Weekly", StringComparison.OrdinalIgnoreCase)
                    ? await _proactiveAgentService.RunWeeklyDigestAsync(force: true, cancellationToken)
                    : await _proactiveAgentService.RunDailyDigestAsync(force: true, cancellationToken);

                if (insight is null)
                {
                    return Ok(new ApiResponse<AgentInsightDto>
                    {
                        Success = true,
                        Message = "Digest was not created (scheduled reports may be disabled).",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<AgentInsightDto>
                {
                    Success = true,
                    Message = $"{type} digest insight created.",
                    Data = insight
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run digest report");
                return StatusCode(500, new ApiResponse<AgentInsightDto>
                {
                    Success = false,
                    Message = "Could not generate digest report.",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }
}
