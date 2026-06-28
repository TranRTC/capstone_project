using System.Text.Json;
using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Infrastructure.Repositories;

namespace IoTMonitoringSystem.API.Services
{
    public interface IAgentAuditService
    {
        Task LogAsync(
            string eventType,
            string username,
            string? userRole = null,
            string? toolName = null,
            string? summary = null,
            object? details = null,
            int? relatedDeviceId = null,
            long? relatedAlertId = null,
            long? sessionId = null,
            int? durationMs = null,
            bool success = true);

        Task LogToolCallAsync(string username, string? userRole, string toolName, bool success, int? durationMs = null, long? sessionId = null);
    }

    public class AgentAuditService : IAgentAuditService
    {
        private readonly IAgentAuditRepository _repository;
        private readonly ILogger<AgentAuditService> _logger;

        public AgentAuditService(IAgentAuditRepository repository, ILogger<AgentAuditService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task LogAsync(
            string eventType,
            string username,
            string? userRole = null,
            string? toolName = null,
            string? summary = null,
            object? details = null,
            int? relatedDeviceId = null,
            long? relatedAlertId = null,
            long? sessionId = null,
            int? durationMs = null,
            bool success = true)
        {
            try
            {
                await _repository.CreateAsync(new AgentAuditLog
                {
                    EventType = eventType,
                    Username = username,
                    UserRole = userRole,
                    ToolName = toolName,
                    Summary = summary,
                    DetailsJson = details is null ? null : JsonSerializer.Serialize(details),
                    RelatedDeviceId = relatedDeviceId,
                    RelatedAlertId = relatedAlertId,
                    SessionId = sessionId,
                    DurationMs = durationMs,
                    Success = success,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write agent audit log for {EventType}", eventType);
            }
        }

        public Task LogToolCallAsync(string username, string? userRole, string toolName, bool success, int? durationMs = null, long? sessionId = null) =>
            LogAsync("ToolCall", username, userRole, toolName, $"Tool {toolName}", success: success, durationMs: durationMs, sessionId: sessionId);
    }
}
