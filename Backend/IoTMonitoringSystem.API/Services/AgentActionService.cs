using System.Security.Claims;
using System.Text.Json;
using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Infrastructure.Repositories;
using IoTMonitoringSystem.Services;

namespace IoTMonitoringSystem.API.Services
{
    public interface IAgentActionService
    {
        Task<AgentActionProposalDto?> GetPendingForUserAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
        Task<AgentActionProposalDto> CreateProposalFromToolCallAsync(
            LlmToolCall toolCall,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default);
        Task<AgentActionResultDto> ConfirmAsync(long proposalId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
        Task<AgentActionProposalDto> CancelAsync(long proposalId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    }

    public class AgentActionService : IAgentActionService
    {
        private readonly IAgentActionProposalRepository _repository;
        private readonly IAlertService _alertService;
        private readonly IDeviceService _deviceService;
        private readonly IActuatorService _actuatorService;
        private readonly AgentActionExecutor _executor;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AgentActionService> _logger;

        public AgentActionService(
            IAgentActionProposalRepository repository,
            IAlertService alertService,
            IDeviceService deviceService,
            IActuatorService actuatorService,
            AgentActionExecutor executor,
            IConfiguration configuration,
            ILogger<AgentActionService> logger)
        {
            _repository = repository;
            _alertService = alertService;
            _deviceService = deviceService;
            _actuatorService = actuatorService;
            _executor = executor;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AgentActionProposalDto?> GetPendingForUserAsync(
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default)
        {
            var username = GetUsername(user);
            if (username is null)
                return null;

            await _repository.ExpireStaleAsync(DateTime.UtcNow);
            var pending = await _repository.GetPendingForUserAsync(username);
            return pending is null ? null : MapToDto(pending, CanWrite(user));
        }

        public async Task<AgentActionProposalDto> CreateProposalFromToolCallAsync(
            LlmToolCall toolCall,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default)
        {
            if (!_configuration.GetValue("Agent:Actions:Enabled", true))
                throw new InvalidOperationException("Agent write actions are disabled.");

            if (!CanWrite(user))
                throw new InvalidOperationException("Your role is read-only. Operators and Admins can confirm write actions.");

            var username = GetUsername(user)
                ?? throw new InvalidOperationException("Could not determine the current user.");

            await _repository.ExpireStaleAsync(DateTime.UtcNow);

            var maxPending = _configuration.GetValue("Agent:Actions:MaxPendingPerUser", 1);
            var existing = await _repository.GetPendingForUserAsync(username);
            if (existing is not null && maxPending <= 1)
                throw new InvalidOperationException("You already have a pending action. Confirm or cancel it before proposing another.");

            var (actionType, payloadJson, summary, relatedAlertId, relatedDeviceId) =
                await BuildProposalAsync(toolCall.Name, toolCall.ArgumentsJson);

            var expiryMinutes = _configuration.GetValue("Agent:Actions:ProposalExpiryMinutes", 10);
            var proposal = new AgentActionProposal
            {
                Username = username,
                UserRole = GetRole(user),
                ActionType = actionType,
                PayloadJson = payloadJson,
                Summary = summary,
                Status = "Pending",
                RelatedAlertId = relatedAlertId,
                RelatedDeviceId = relatedDeviceId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes)
            };

            proposal = await _repository.CreateAsync(proposal);
            _logger.LogInformation("Created agent action proposal {Id} ({ActionType}) for {User}", proposal.AgentActionProposalId, actionType, username);
            return MapToDto(proposal, canConfirm: true);
        }

        public async Task<AgentActionResultDto> ConfirmAsync(
            long proposalId,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default)
        {
            if (!CanWrite(user))
                throw new InvalidOperationException("Your role is read-only.");

            var username = GetUsername(user)
                ?? throw new InvalidOperationException("Could not determine the current user.");

            var proposal = await _repository.GetByIdAsync(proposalId)
                ?? throw new KeyNotFoundException($"Action proposal {proposalId} not found.");

            if (!string.Equals(proposal.Username, username, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("You can only confirm your own action proposals.");

            if (proposal.Status == "Executed")
                throw new InvalidOperationException("This action was already executed.");

            if (proposal.Status != "Pending")
                throw new InvalidOperationException($"This action is no longer pending (status: {proposal.Status}).");

            if (proposal.ExpiresAt <= DateTime.UtcNow)
            {
                proposal.Status = "Expired";
                await _repository.UpdateAsync(proposal);
                throw new InvalidOperationException("This action proposal has expired. Please ask the assistant again.");
            }

            try
            {
                var resultJson = await _executor.ExecuteAsync(proposal.ActionType, proposal.PayloadJson, username);
                var message = await _executor.BuildSuccessMessageAsync(proposal.ActionType, proposal.PayloadJson, resultJson);

                proposal.Status = "Executed";
                proposal.ResultJson = resultJson;
                proposal.ExecutedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(proposal);

                return new AgentActionResultDto
                {
                    AgentActionProposalId = proposal.AgentActionProposalId,
                    ActionType = proposal.ActionType,
                    Status = proposal.Status,
                    Message = message,
                    ResultJson = resultJson
                };
            }
            catch (Exception ex)
            {
                proposal.Status = "Failed";
                proposal.ResultJson = JsonSerializer.Serialize(new { error = ex.Message });
                await _repository.UpdateAsync(proposal);
                _logger.LogWarning(ex, "Agent action {Id} failed", proposalId);
                throw;
            }
        }

        public async Task<AgentActionProposalDto> CancelAsync(
            long proposalId,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default)
        {
            var username = GetUsername(user)
                ?? throw new InvalidOperationException("Could not determine the current user.");

            var proposal = await _repository.GetByIdAsync(proposalId)
                ?? throw new KeyNotFoundException($"Action proposal {proposalId} not found.");

            if (!string.Equals(proposal.Username, username, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("You can only cancel your own action proposals.");

            if (proposal.Status != "Pending")
                throw new InvalidOperationException($"This action is not pending (status: {proposal.Status}).");

            proposal.Status = "Cancelled";
            await _repository.UpdateAsync(proposal);
            return MapToDto(proposal, canConfirm: false);
        }

        private async Task<(string ActionType, string PayloadJson, string Summary, long? RelatedAlertId, int? RelatedDeviceId)>
            BuildProposalAsync(string toolName, string argumentsJson)
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
            var root = doc.RootElement;

            return toolName switch
            {
                "propose_acknowledge_alert" => await BuildAlertProposalAsync("AcknowledgeAlert", root, "Acknowledge"),
                "propose_resolve_alert" => await BuildAlertProposalAsync("ResolveAlert", root, "Resolve"),
                "propose_create_device" => BuildCreateDeviceProposal(root),
                "propose_send_device_command" => await BuildSendDeviceCommandProposalAsync(root),
                "propose_toggle_actuator" => await BuildToggleActuatorProposalAsync(root),
                _ => throw new InvalidOperationException($"Unknown propose tool '{toolName}'.")
            };
        }

        private async Task<(string, string, string, long?, int?)> BuildAlertProposalAsync(
            string actionType,
            JsonElement root,
            string verb)
        {
            if (!root.TryGetProperty("alertId", out var idProp) || !idProp.TryGetInt64(out var alertId))
                throw new ArgumentException("Missing or invalid alertId.");

            var alert = await _alertService.GetAlertByIdAsync(alertId);
            var payload = JsonSerializer.Serialize(new { alertId });
            var summary =
                $"{verb} alert #{alert.AlertId} ({alert.Severity}): {alert.Message} on device {alert.DeviceId}.";
            return (actionType, payload, summary, alert.AlertId, alert.DeviceId);
        }

        private static (string, string, string, long?, int?) BuildCreateDeviceProposal(JsonElement root)
        {
            if (!root.TryGetProperty("deviceName", out var nameProp) || string.IsNullOrWhiteSpace(nameProp.GetString()))
                throw new ArgumentException("deviceName is required.");
            if (!root.TryGetProperty("deviceType", out var typeProp) || string.IsNullOrWhiteSpace(typeProp.GetString()))
                throw new ArgumentException("deviceType is required.");

            var dto = new CreateDeviceDto
            {
                DeviceName = nameProp.GetString()!.Trim(),
                DeviceType = typeProp.GetString()!.Trim(),
                Location = root.TryGetProperty("location", out var loc) ? loc.GetString()?.Trim() : null,
                Description = root.TryGetProperty("description", out var desc) ? desc.GetString()?.Trim() : null,
                EdgeDeviceId = root.TryGetProperty("edgeDeviceId", out var edge) ? edge.GetString()?.Trim() : null
            };

            var payload = JsonSerializer.Serialize(new
            {
                deviceName = dto.DeviceName,
                deviceType = dto.DeviceType,
                location = dto.Location,
                description = dto.Description,
                edgeDeviceId = dto.EdgeDeviceId
            });

            var summary = $"Create device \"{dto.DeviceName}\" (type: {dto.DeviceType})"
                + (string.IsNullOrWhiteSpace(dto.Location) ? "." : $" at {dto.Location}.");
            return ("CreateDevice", payload, summary, null, null);
        }

        private async Task<(string, string, string, long?, int?)> BuildSendDeviceCommandProposalAsync(JsonElement root)
        {
            if (!root.TryGetProperty("deviceId", out var deviceProp) || !deviceProp.TryGetInt32(out var deviceId))
                throw new ArgumentException("deviceId is required.");
            if (!root.TryGetProperty("actuatorId", out var actuatorProp) || !actuatorProp.TryGetInt32(out var actuatorId))
                throw new ArgumentException("actuatorId is required.");
            if (!root.TryGetProperty("commandType", out var typeProp) || string.IsNullOrWhiteSpace(typeProp.GetString()))
                throw new ArgumentException("commandType is required (SetPower or SetValue).");

            var commandType = typeProp.GetString()!.Trim();
            var device = await _deviceService.GetDeviceByIdAsync(deviceId);
            var actuators = await _actuatorService.GetByDeviceIdAsync(deviceId);
            var actuator = actuators.FirstOrDefault(a => a.ActuatorId == actuatorId)
                ?? throw new KeyNotFoundException($"Actuator {actuatorId} not found on device {deviceId}.");

            string payloadJson;
            string actionDescription;

            if (commandType.Equals("SetPower", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(actuator.Kind, "Discrete", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Actuator {actuatorId} is {actuator.Kind}; SetPower requires a Discrete actuator.");

                if (!root.TryGetProperty("on", out var onProp) ||
                    (onProp.ValueKind != JsonValueKind.True && onProp.ValueKind != JsonValueKind.False))
                    throw new ArgumentException("SetPower requires boolean property 'on'.");

                var on = onProp.GetBoolean();
                payloadJson = JsonSerializer.Serialize(new { on });
                actionDescription = on ? "Turn ON" : "Turn OFF";
            }
            else if (commandType.Equals("SetValue", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(actuator.Kind, "Analog", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Actuator {actuatorId} is {actuator.Kind}; SetValue requires an Analog actuator.");

                if (!root.TryGetProperty("value", out var valueProp) || valueProp.ValueKind != JsonValueKind.Number)
                    throw new ArgumentException("SetValue requires numeric property 'value'.");

                var value = valueProp.GetDecimal();
                payloadJson = JsonSerializer.Serialize(new { value });
                actionDescription = $"Set value to {value}";
            }
            else
            {
                throw new ArgumentException("commandType must be SetPower or SetValue.");
            }

            var payload = JsonSerializer.Serialize(new
            {
                deviceId,
                actuatorId,
                commandType,
                payload = payloadJson
            });

            var summary =
                $"{actionDescription} actuator \"{actuator.Name}\" (ID {actuatorId}) on device \"{device.DeviceName}\" (ID {deviceId}). " +
                $"Current state: {ActuatorStateHelper.DescribeState(actuator.LastKnownState)}.";
            return ("SendDeviceCommand", payload, summary, null, deviceId);
        }

        private async Task<(string, string, string, long?, int?)> BuildToggleActuatorProposalAsync(JsonElement root)
        {
            if (!root.TryGetProperty("deviceId", out var deviceProp) || !deviceProp.TryGetInt32(out var deviceId))
                throw new ArgumentException("deviceId is required.");
            if (!root.TryGetProperty("actuatorId", out var actuatorProp) || !actuatorProp.TryGetInt32(out var actuatorId))
                throw new ArgumentException("actuatorId is required.");

            var device = await _deviceService.GetDeviceByIdAsync(deviceId);
            var actuators = await _actuatorService.GetByDeviceIdAsync(deviceId);
            var actuator = actuators.FirstOrDefault(a => a.ActuatorId == actuatorId)
                ?? throw new KeyNotFoundException($"Actuator {actuatorId} not found on device {deviceId}.");

            if (!string.Equals(actuator.Kind, "Discrete", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Actuator {actuatorId} is {actuator.Kind}; toggle applies only to Discrete actuators.");

            var turnOn = ActuatorStateHelper.ToggleTargetIsOn(actuator.LastKnownState);
            var currentState = ActuatorStateHelper.DescribeState(actuator.LastKnownState);
            var payloadJson = JsonSerializer.Serialize(new { on = turnOn });
            var actionDescription = turnOn ? "Turn ON" : "Turn OFF";

            var payload = JsonSerializer.Serialize(new
            {
                deviceId,
                actuatorId,
                commandType = "SetPower",
                payload = payloadJson
            });

            var summary =
                $"{actionDescription} actuator \"{actuator.Name}\" (ID {actuatorId}) on device \"{device.DeviceName}\" (ID {deviceId}). " +
                $"Current state: {currentState}.";
            return ("SendDeviceCommand", payload, summary, null, deviceId);
        }

        private static AgentActionProposalDto MapToDto(AgentActionProposal proposal, bool canConfirm) =>
            new()
            {
                AgentActionProposalId = proposal.AgentActionProposalId,
                ActionType = proposal.ActionType,
                Summary = proposal.Summary,
                Status = proposal.Status,
                RelatedAlertId = proposal.RelatedAlertId,
                RelatedDeviceId = proposal.RelatedDeviceId,
                CreatedAt = proposal.CreatedAt,
                ExpiresAt = proposal.ExpiresAt,
                CanConfirm = canConfirm && proposal.Status == "Pending" && proposal.ExpiresAt > DateTime.UtcNow
            };

        private static bool CanWrite(ClaimsPrincipal user)
        {
            var role = GetRole(user);
            return role is "Admin" or "Operator";
        }

        private static string GetRole(ClaimsPrincipal user) =>
            user.FindFirst("role")?.Value
            ?? user.FindFirst(ClaimTypes.Role)?.Value
            ?? "Viewer";

        private static string? GetUsername(ClaimsPrincipal user) =>
            user.FindFirst("unique_name")?.Value
            ?? user.Identity?.Name;
    }
}
