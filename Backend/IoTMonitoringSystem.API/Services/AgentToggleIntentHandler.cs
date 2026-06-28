using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Services;

namespace IoTMonitoringSystem.API.Services
{
    /// <summary>
    /// Parses natural-language actuator commands and creates proposals without relying on the LLM tool loop.
    /// </summary>
    public partial class AgentToggleIntentHandler
    {
        private readonly IDeviceService _deviceService;
        private readonly IActuatorService _actuatorService;
        private readonly IAgentActionService _agentActionService;
        private readonly ILogger<AgentToggleIntentHandler> _logger;

        public AgentToggleIntentHandler(
            IDeviceService deviceService,
            IActuatorService actuatorService,
            IAgentActionService agentActionService,
            ILogger<AgentToggleIntentHandler> logger)
        {
            _deviceService = deviceService;
            _actuatorService = actuatorService;
            _agentActionService = agentActionService;
            _logger = logger;
        }

        public static bool LooksLikeToggleRequest(string? message) =>
            !string.IsNullOrWhiteSpace(message) &&
            ToggleKeywordPattern().IsMatch(NormalizeMessage(message));

        public static bool LooksLikeTurnOnOffRequest(string? message) =>
            !string.IsNullOrWhiteSpace(message) &&
            TurnOnOffPattern().IsMatch(NormalizeMessage(message));

        public static bool LooksLikeActuatorCommandRequest(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return false;

            var normalized = NormalizeMessage(message);
            if (LooksLikeToggleRequest(normalized) || LooksLikeTurnOnOffRequest(normalized))
                return true;

            // "turn of actuator..." typo or "turn actuator 1 device 1" without on/off
            return TurnOrToggleWithActuatorPattern().IsMatch(normalized);
        }

        public static string NormalizeMessage(string message)
        {
            var m = message.Trim();
            m = TurnOfTypoPattern().Replace(m, "turn off");
            return m;
        }

        public sealed class ActuatorCommandIntentResult
        {
            public AgentActionProposalDto? Proposal { get; init; }
            public string? Reply { get; init; }
            public bool Handled => Proposal is not null || !string.IsNullOrWhiteSpace(Reply);
        }

        public async Task<ActuatorCommandIntentResult> ResolveActuatorCommandAsync(
            string message,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default)
        {
            if (!LooksLikeActuatorCommandRequest(message))
                return new ActuatorCommandIntentResult();

            message = NormalizeMessage(message);

            try
            {
                if (LooksLikeToggleRequest(message))
                {
                    var proposal = await TryCreateToggleProposalAsync(message, user, cancellationToken);
                    return proposal is null
                        ? await BuildGuidanceResultAsync(message, cancellationToken)
                        : new ActuatorCommandIntentResult { Proposal = proposal };
                }

                return await ResolveTurnOnOffAsync(message, user, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return new ActuatorCommandIntentResult { Reply = ex.Message };
            }
        }

        public async Task<AgentActionProposalDto?> TryCreateActuatorCommandProposalAsync(
            string message,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default)
        {
            var result = await ResolveActuatorCommandAsync(message, user, cancellationToken);
            return result.Proposal;
        }

        public async Task<AgentActionProposalDto?> TryCreateToggleProposalAsync(
            string message,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default)
        {
            if (!LooksLikeToggleRequest(message))
                return null;

            try
            {
                var deviceId = await ResolveDeviceIdAsync(message);
                if (deviceId is null)
                    return null;

                var actuatorId = await ResolveActuatorIdAsync(message, deviceId.Value);
                if (actuatorId is null)
                    return null;

                var toolCall = new LlmToolCall
                {
                    Id = $"direct_{Guid.NewGuid():N}"[..16],
                    Name = "propose_toggle_actuator",
                    ArgumentsJson = JsonSerializer.Serialize(new { deviceId = deviceId.Value, actuatorId = actuatorId.Value })
                };

                var proposal = await _agentActionService.CreateProposalFromToolCallAsync(
                    toolCall, user, cancellationToken);
                _logger.LogInformation(
                    "Created toggle proposal {Id} via direct intent (device {DeviceId}, actuator {ActuatorId})",
                    proposal.AgentActionProposalId, deviceId, actuatorId);
                return proposal;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Direct toggle intent could not create proposal for message: {Message}", message);
                return null;
            }
        }

        private async Task<ActuatorCommandIntentResult> ResolveTurnOnOffAsync(
            string message,
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
        {
            var turnOn = TryParseTurnOn(message);
            if (turnOn is null)
                return new ActuatorCommandIntentResult();

            var deviceId = await ResolveDeviceIdAsync(message);
            if (deviceId is null)
                return await BuildGuidanceResultAsync(message, cancellationToken);

            var actuatorId = await ResolveActuatorIdAsync(message, deviceId.Value);
            if (actuatorId is null)
                return await BuildGuidanceForDeviceAsync(deviceId.Value, message, cancellationToken);

            var actuators = await _actuatorService.GetByDeviceIdAsync(deviceId.Value);
            var actuator = actuators.FirstOrDefault(a => a.ActuatorId == actuatorId.Value);
            if (actuator is null)
                return await BuildGuidanceForDeviceAsync(deviceId.Value, message, cancellationToken);

            var currentIsOn = ActuatorStateHelper.TryParseIsOn(actuator.LastKnownState);
            var currentState = ActuatorStateHelper.DescribeState(actuator.LastKnownState);

            if (currentIsOn == turnOn.Value)
            {
                var already = turnOn.Value ? "on" : "off";
                return new ActuatorCommandIntentResult
                {
                    Reply =
                        $"Actuator \"{actuator.Name}\" (ID {actuator.ActuatorId}) on device {deviceId} is already {already} " +
                        $"(LastKnownState: {currentState}). No command is needed."
                };
            }

            var toolCall = new LlmToolCall
            {
                Id = $"direct_{Guid.NewGuid():N}"[..16],
                Name = "propose_send_device_command",
                ArgumentsJson = JsonSerializer.Serialize(new
                {
                    deviceId = deviceId.Value,
                    actuatorId = actuatorId.Value,
                    commandType = "SetPower",
                    on = turnOn.Value
                })
            };

            var proposal = await _agentActionService.CreateProposalFromToolCallAsync(
                toolCall, user, cancellationToken);
            _logger.LogInformation(
                "Created SetPower proposal {Id} via direct intent (device {DeviceId}, actuator {ActuatorId}, on={On})",
                proposal.AgentActionProposalId, deviceId, actuatorId, turnOn);

            return new ActuatorCommandIntentResult { Proposal = proposal };
        }

        private async Task<ActuatorCommandIntentResult> BuildGuidanceResultAsync(
            string message,
            CancellationToken cancellationToken)
        {
            var devices = await _deviceService.GetAllDevicesAsync();
            if (devices.Count == 0)
            {
                return new ActuatorCommandIntentResult
                {
                    Reply = "No devices are registered. Add a device before sending actuator commands."
                };
            }

            if (devices.Count == 1)
                return await BuildGuidanceForDeviceAsync(devices[0].DeviceId, message, cancellationToken);

            var sb = new StringBuilder();
            sb.AppendLine("I need a device ID to control an actuator. Registered devices:");
            foreach (var d in devices)
                sb.AppendLine($"- Device {d.DeviceId}: {d.DeviceName}");
            sb.AppendLine();
            sb.AppendLine("Example: \"Turn off actuator 1 on device 1\"");

            return new ActuatorCommandIntentResult { Reply = sb.ToString().Trim() };
        }

        private async Task<ActuatorCommandIntentResult> BuildGuidanceForDeviceAsync(
            int deviceId,
            string message,
            CancellationToken cancellationToken)
        {
            var device = await _deviceService.GetDeviceByIdAsync(deviceId);
            var actuators = await _actuatorService.GetByDeviceIdAsync(deviceId);

            if (actuators.Count == 0)
            {
                return new ActuatorCommandIntentResult
                {
                    Reply = $"Device \"{device.DeviceName}\" (ID {deviceId}) has no actuators configured."
                };
            }

            if (actuators.Count == 1 && LooksLikeActuatorCommandRequest(message))
            {
                return new ActuatorCommandIntentResult
                {
                    Reply =
                        $"Use actuator ID {actuators[0].ActuatorId} (\"{actuators[0].Name}\", state: {ActuatorStateHelper.DescribeState(actuators[0].LastKnownState)}). " +
                        $"Example: \"Turn off actuator {actuators[0].ActuatorId} on device {deviceId}\""
                };
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Actuators on device \"{device.DeviceName}\" (ID {deviceId}):");
            foreach (var a in actuators)
            {
                var state = ActuatorStateHelper.DescribeState(a.LastKnownState);
                sb.AppendLine($"- Actuator {a.ActuatorId}: \"{a.Name}\" — current state: {state}");
            }
            sb.AppendLine();
            sb.AppendLine("Specify the actuator ID, for example: \"Turn off actuator 1 on device 1\"");

            return new ActuatorCommandIntentResult { Reply = sb.ToString().Trim() };
        }

        private static bool? TryParseTurnOn(string message)
        {
            if (TurnOnPattern().IsMatch(message))
                return true;
            if (TurnOffPattern().IsMatch(message))
                return false;
            return null;
        }

        private async Task<int?> ResolveDeviceIdAsync(string message)
        {
            var ofDeviceMatch = OfDeviceIdPattern().Match(message);
            if (ofDeviceMatch.Success && int.TryParse(ofDeviceMatch.Groups["id"].Value, out var ofDeviceId))
                return ofDeviceId;

            var idMatch = DeviceIdPattern().Match(message);
            if (idMatch.Success && int.TryParse(idMatch.Groups["id"].Value, out var deviceId))
                return deviceId;

            var nameMatch = DeviceNamePattern().Match(message);
            if (nameMatch.Success)
            {
                var name = nameMatch.Groups["name"].Value.Trim();
                var devices = await _deviceService.GetAllDevicesAsync();
                var match = devices.FirstOrDefault(d =>
                    d.DeviceName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    ?? devices.FirstOrDefault(d =>
                        d.DeviceName.Contains(name, StringComparison.OrdinalIgnoreCase));
                if (match is not null)
                    return match.DeviceId;
            }

            if (LooksLikeActuatorCommandRequest(message))
            {
                var devices = await _deviceService.GetAllDevicesAsync();
                var active = devices.Where(d => d.IsActive).ToList();
                if (active.Count == 1)
                    return active[0].DeviceId;
            }

            return null;
        }

        private async Task<int?> ResolveActuatorIdAsync(string message, int deviceId)
        {
            var ofActuatorMatch = OfActuatorIdPattern().Match(message);
            if (ofActuatorMatch.Success && int.TryParse(ofActuatorMatch.Groups["id"].Value, out var ofActuatorId))
                return ofActuatorId;

            var idMatch = ActuatorIdPattern().Match(message);
            if (idMatch.Success && int.TryParse(idMatch.Groups["id"].Value, out var actuatorId))
                return actuatorId;

            var actuators = await _actuatorService.GetByDeviceIdAsync(deviceId);
            if (actuators.Count == 0)
                return null;

            if (actuators.Count == 1 && LooksLikeActuatorCommandRequest(message))
                return actuators[0].ActuatorId;

            if (TheActuatorPhrasePattern().IsMatch(message) && actuators.Count == 1)
                return actuators[0].ActuatorId;

            var nameMatch = ActuatorNamePattern().Match(message);
            if (nameMatch.Success)
            {
                var name = nameMatch.Groups["name"].Value.Trim();
                var byName = FindActuatorByName(actuators, name);
                if (byName is not null)
                    return byName.ActuatorId;
            }

            var reversedNameMatch = ActuatorNameBeforeWordPattern().Match(message);
            if (reversedNameMatch.Success)
            {
                var name = reversedNameMatch.Groups["name"].Value.Trim();
                var byName = FindActuatorByName(actuators, name);
                if (byName is not null)
                    return byName.ActuatorId;
            }

            return null;
        }

        private static ActuatorDto? FindActuatorByName(IReadOnlyList<ActuatorDto> actuators, string name) =>
            actuators.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? actuators.FirstOrDefault(a => a.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

        [GeneratedRegex(@"\bturn\s+of\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex TurnOfTypoPattern();

        [GeneratedRegex(@"\b(turn|toggle)\b.*\bactuator\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex TurnOrToggleWithActuatorPattern();

        [GeneratedRegex(@"\btoggle\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex ToggleKeywordPattern();

        [GeneratedRegex(@"\bturn\s+(?:the\s+)?(?:actuator\s+)?(?:on|off)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex TurnOnOffPattern();

        [GeneratedRegex(@"\bturn\s+(?:the\s+)?(?:actuator\s+)?on\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex TurnOnPattern();

        [GeneratedRegex(@"\bturn\s+(?:the\s+)?(?:actuator\s+)?off\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex TurnOffPattern();

        [GeneratedRegex(@"\bthe\s+actuator\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex TheActuatorPhrasePattern();

        [GeneratedRegex(@"\bof\s+device\s+(?:id\s*)?(?<id>\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex OfDeviceIdPattern();

        [GeneratedRegex(@"\bdevice\s+(?:id\s*)?(?<id>\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex DeviceIdPattern();

        [GeneratedRegex(@"device\s+(?:named\s+)?[""']?(?<name>[^""'\d][^""']*?)[""']?(?:\s*$|\s+and|\s+with|\s*,)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex DeviceNamePattern();

        [GeneratedRegex(@"\bof\s+actuator\s+(?:id\s*)?(?<id>\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex OfActuatorIdPattern();

        [GeneratedRegex(@"\bactuator\s+(?:id\s*)?(?<id>\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex ActuatorIdPattern();

        [GeneratedRegex(@"actuator\s+(?:named\s+)?[""']?(?<name>[^""']+?)[""']?\s+on\s+device", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex ActuatorNamePattern();

        [GeneratedRegex(@"(?:toggle\s+(?:the\s+)?|turn\s+(?:on|off)\s+(?:the\s+)?)(?<name>[A-Za-z][A-Za-z0-9_-]*)\s+actuator", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex ActuatorNameBeforeWordPattern();
    }
}
