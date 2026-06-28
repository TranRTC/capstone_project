using System.Text.Json;
using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Services;

namespace IoTMonitoringSystem.API.Services
{
    public class AgentActionExecutor
    {
        private readonly IAlertService _alertService;
        private readonly IDeviceService _deviceService;
        private readonly IDeviceCommandService _deviceCommandService;

        public AgentActionExecutor(
            IAlertService alertService,
            IDeviceService deviceService,
            IDeviceCommandService deviceCommandService)
        {
            _alertService = alertService;
            _deviceService = deviceService;
            _deviceCommandService = deviceCommandService;
        }

        public async Task<string> ExecuteAsync(string actionType, string payloadJson, string? requestedBy = null)
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson);
            var root = doc.RootElement;

            return actionType switch
            {
                "AcknowledgeAlert" => JsonSerializer.Serialize(await _alertService.AcknowledgeAlertAsync(GetAlertId(root))),
                "ResolveAlert" => JsonSerializer.Serialize(await _alertService.ResolveAlertAsync(GetAlertId(root))),
                "CreateDevice" => JsonSerializer.Serialize(await _deviceService.CreateDeviceAsync(BuildCreateDeviceDto(root))),
                "SendDeviceCommand" => JsonSerializer.Serialize(await ExecuteSendDeviceCommandAsync(root, requestedBy)),
                _ => throw new InvalidOperationException($"Unsupported action type '{actionType}'.")
            };
        }

        public Task<string> BuildSuccessMessageAsync(string actionType, string payloadJson, string resultJson)
        {
            return Task.FromResult(actionType switch
            {
                "AcknowledgeAlert" => $"Alert {GetAlertIdFromPayload(payloadJson)} acknowledged successfully.",
                "ResolveAlert" => $"Alert {GetAlertIdFromPayload(payloadJson)} resolved successfully.",
                "CreateDevice" =>
                    TryGetDeviceNameFromResult(resultJson, out var name)
                        ? $"Device \"{name}\" created successfully."
                        : "Device created successfully.",
                "SendDeviceCommand" => BuildSendCommandSuccessMessage(payloadJson, resultJson),
                _ => "Action completed successfully."
            });
        }

        private async Task<DeviceCommandDto> ExecuteSendDeviceCommandAsync(JsonElement root, string? requestedBy)
        {
            var deviceId = GetRequiredInt(root, "deviceId");
            var dto = new CreateDeviceCommandDto
            {
                ActuatorId = GetRequiredInt(root, "actuatorId"),
                CommandType = GetRequiredString(root, "commandType"),
                Payload = GetRequiredString(root, "payload"),
                CorrelationId = TryGetString(root, "correlationId")
            };

            return await _deviceCommandService.CreateCommandAsync(deviceId, dto, requestedBy);
        }

        private static string BuildSendCommandSuccessMessage(string payloadJson, string resultJson)
        {
            using var payloadDoc = JsonDocument.Parse(payloadJson);
            var root = payloadDoc.RootElement;
            var commandType = TryGetString(root, "commandType") ?? "command";
            var deviceId = root.TryGetProperty("deviceId", out var deviceProp) && deviceProp.TryGetInt32(out var d)
                ? d
                : (int?)null;
            var actuatorId = root.TryGetProperty("actuatorId", out var actuatorProp) && actuatorProp.TryGetInt32(out var a)
                ? a
                : (int?)null;

            long? commandId = null;
            try
            {
                using var resultDoc = JsonDocument.Parse(resultJson);
                if (resultDoc.RootElement.TryGetProperty("commandId", out var idProp) && idProp.TryGetInt64(out var id))
                    commandId = id;
            }
            catch
            {
                // Best-effort for user-facing text.
            }

            var target = deviceId.HasValue && actuatorId.HasValue
                ? $"device {deviceId}, actuator {actuatorId}"
                : "the actuator";

            return commandId.HasValue
                ? $"{commandType} command #{commandId} sent to {target}."
                : $"{commandType} command sent to {target}.";
        }

        private static long GetAlertId(JsonElement root)
        {
            if (!root.TryGetProperty("alertId", out var prop) || !prop.TryGetInt64(out var id))
                throw new ArgumentException("Missing or invalid alertId.");
            return id;
        }

        private static long GetAlertIdFromPayload(string payloadJson)
        {
            using var doc = JsonDocument.Parse(payloadJson);
            return GetAlertId(doc.RootElement);
        }

        private static CreateDeviceDto BuildCreateDeviceDto(JsonElement root)
        {
            var name = GetRequiredString(root, "deviceName");
            var type = GetRequiredString(root, "deviceType");
            return new CreateDeviceDto
            {
                DeviceName = name,
                DeviceType = type,
                Location = TryGetString(root, "location"),
                Description = TryGetString(root, "description"),
                EdgeDeviceId = TryGetString(root, "edgeDeviceId")
            };
        }

        private static int GetRequiredInt(JsonElement root, string name)
        {
            if (!root.TryGetProperty(name, out var prop) || !prop.TryGetInt32(out var value))
                throw new ArgumentException($"Missing or invalid '{name}'.");
            return value;
        }

        private static string GetRequiredString(JsonElement root, string name)
        {
            if (!root.TryGetProperty(name, out var prop) || string.IsNullOrWhiteSpace(prop.GetString()))
                throw new ArgumentException($"Missing or invalid '{name}'.");
            return prop.GetString()!.Trim();
        }

        private static string? TryGetString(JsonElement root, string name)
        {
            if (!root.TryGetProperty(name, out var prop))
                return null;
            var value = prop.GetString()?.Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static bool TryGetDeviceNameFromResult(string resultJson, out string deviceName)
        {
            deviceName = string.Empty;
            try
            {
                using var doc = JsonDocument.Parse(resultJson);
                if (doc.RootElement.TryGetProperty("deviceName", out var prop))
                {
                    deviceName = prop.GetString() ?? string.Empty;
                    return !string.IsNullOrWhiteSpace(deviceName);
                }
            }
            catch
            {
                // Best-effort parsing for user-facing success text only.
            }

            return false;
        }
    }
}
