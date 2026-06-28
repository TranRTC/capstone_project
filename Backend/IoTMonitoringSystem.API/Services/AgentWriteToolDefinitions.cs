namespace IoTMonitoringSystem.API.Services
{
    public static class AgentWriteToolDefinitions
    {
        public static readonly HashSet<string> ProposeToolNames = new(StringComparer.Ordinal)
        {
            "propose_acknowledge_alert",
            "propose_resolve_alert",
            "propose_create_device",
            "propose_send_device_command",
            "propose_toggle_actuator"
        };

        public static bool IsProposeTool(string toolName) => ProposeToolNames.Contains(toolName);

        public static List<LlmToolDefinition> BuildProposeTools() =>
            new()
            {
                new LlmToolDefinition
                {
                    Name = "propose_acknowledge_alert",
                    Description = "Propose acknowledging an active alert. Requires user confirmation before execution. Admin/Operator only.",
                    ParametersSchema = new
                    {
                        type = "object",
                        properties = new { alertId = new { type = "integer", description = "Alert ID" } },
                        required = new[] { "alertId" }
                    }
                },
                new LlmToolDefinition
                {
                    Name = "propose_resolve_alert",
                    Description = "Propose resolving an alert. Requires user confirmation before execution. Admin/Operator only.",
                    ParametersSchema = new
                    {
                        type = "object",
                        properties = new { alertId = new { type = "integer", description = "Alert ID" } },
                        required = new[] { "alertId" }
                    }
                },
                new LlmToolDefinition
                {
                    Name = "propose_create_device",
                    Description = "Propose registering a new IoT device. Requires user confirmation. Admin/Operator only.",
                    ParametersSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            deviceName = new { type = "string" },
                            deviceType = new { type = "string" },
                            location = new { type = "string" },
                            description = new { type = "string" },
                            edgeDeviceId = new { type = "string" }
                        },
                        required = new[] { "deviceName", "deviceType" }
                    }
                },
                new LlmToolDefinition
                {
                    Name = "propose_send_device_command",
                    Description = "Propose sending an actuator command (SetPower on/off or SetValue for analog). Requires user confirmation. Admin/Operator only. Use get_actuators_by_device first.",
                    ParametersSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            deviceId = new { type = "integer", description = "Device ID" },
                            actuatorId = new { type = "integer", description = "Actuator ID on the device" },
                            commandType = new { type = "string", description = "SetPower or SetValue" },
                            on = new { type = "boolean", description = "Required for SetPower (true=on, false=off)" },
                            value = new { type = "number", description = "Required for SetValue (analog actuators)" }
                        },
                        required = new[] { "deviceId", "actuatorId", "commandType" }
                    }
                },
                new LlmToolDefinition
                {
                    Name = "propose_toggle_actuator",
                    Description = "Propose toggling a discrete actuator (on->off or off->on). Server reads LastKnownState automatically. Requires user confirmation. Admin/Operator only. Prefer this when the user says toggle.",
                    ParametersSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            deviceId = new { type = "integer", description = "Device ID" },
                            actuatorId = new { type = "integer", description = "Actuator ID on the device" }
                        },
                        required = new[] { "deviceId", "actuatorId" }
                    }
                }
            };
    }
}
