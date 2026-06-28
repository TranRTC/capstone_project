using System.Text.Json;
using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Infrastructure.Repositories;

namespace IoTMonitoringSystem.API.Services
{
    public interface IAgentChatSessionService
    {
        Task<AgentChatSession> GetOrCreateSessionAsync(string username, long? sessionId, AgentChatContextDto? context);
        Task AppendMessagesAsync(long sessionId, string username, string userMessage, string assistantReply, IReadOnlyList<string> toolsUsed, DateTime? dataAsOfUtc);
        Task<AgentChatSessionDto?> GetSessionDtoAsync(long sessionId, string username);
    }

    public class AgentChatSessionService : IAgentChatSessionService
    {
        private readonly IAgentChatSessionRepository _repository;

        public AgentChatSessionService(IAgentChatSessionRepository repository) => _repository = repository;

        public async Task<AgentChatSession> GetOrCreateSessionAsync(string username, long? sessionId, AgentChatContextDto? context)
        {
            if (sessionId.HasValue)
            {
                var existing = await _repository.GetByIdForUserAsync(sessionId.Value, username);
                if (existing is not null)
                {
                    if (context is not null)
                    {
                        existing.ContextDeviceId = context.DeviceId ?? existing.ContextDeviceId;
                        existing.ContextAlertId = context.AlertId ?? existing.ContextAlertId;
                        existing.ContextRoute = context.Route ?? existing.ContextRoute;
                        await _repository.UpdateAsync(existing);
                    }
                    return existing;
                }
            }

            var session = new AgentChatSession
            {
                Username = username,
                Title = BuildTitle(context),
                ContextDeviceId = context?.DeviceId,
                ContextAlertId = context?.AlertId,
                ContextRoute = context?.Route,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            return await _repository.CreateAsync(session);
        }

        public async Task AppendMessagesAsync(
            long sessionId,
            string username,
            string userMessage,
            string assistantReply,
            IReadOnlyList<string> toolsUsed,
            DateTime? dataAsOfUtc)
        {
            var session = await _repository.GetByIdForUserAsync(sessionId, username);
            if (session is null)
                return;

            var toolsJson = toolsUsed.Count > 0 ? JsonSerializer.Serialize(toolsUsed) : null;
            session.Messages.Add(new AgentChatMessage
            {
                Role = "user",
                Content = userMessage,
                CreatedAt = DateTime.UtcNow
            });
            session.Messages.Add(new AgentChatMessage
            {
                Role = "assistant",
                Content = assistantReply,
                ToolsUsedJson = toolsJson,
                DataAsOfUtc = dataAsOfUtc,
                CreatedAt = DateTime.UtcNow
            });
            await _repository.UpdateAsync(session);
        }

        public async Task<AgentChatSessionDto?> GetSessionDtoAsync(long sessionId, string username)
        {
            var session = await _repository.GetByIdForUserAsync(sessionId, username);
            if (session is null)
                return null;

            return new AgentChatSessionDto
            {
                SessionId = session.AgentChatSessionId,
                Title = session.Title,
                Context = new AgentChatContextDto
                {
                    DeviceId = session.ContextDeviceId,
                    AlertId = session.ContextAlertId,
                    Route = session.ContextRoute
                },
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt,
                Messages = session.Messages.Select(m => new AgentChatMessageDto
                {
                    Role = m.Role,
                    Content = m.Content
                }).ToList()
            };
        }

        private static string? BuildTitle(AgentChatContextDto? context)
        {
            if (context?.DeviceId is int deviceId)
                return $"Device {deviceId} chat";
            return "Assistant chat";
        }
    }
}
