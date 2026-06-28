namespace IoTMonitoringSystem.API.Services
{
    public static class AgentToolDefinitions
    {
        public static List<LlmToolDefinition> BuildReadOnlyTools() =>
            new()
            {
                new LlmToolDefinition
                {
                    Name = "get_devices",
                    Description = "Get all registered IoT devices with id, name, type, location, and activity status.",
                    ParametersSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
                },
                new LlmToolDefinition
                {
                    Name = "get_device",
                    Description = "Get one device by deviceId including status and last seen time.",
                    ParametersSchema = new
                    {
                        type = "object",
                        properties = new { deviceId = new { type = "integer", description = "Device ID" } },
                        required = new[] { "deviceId" }
                    }
                },
                new LlmToolDefinition
                {
                    Name = "get_active_alerts",
                    Description = "Get all currently active alerts.",
                    ParametersSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
                },
                new LlmToolDefinition
                {
                    Name = "get_alerts",
                    Description = "Get alerts with optional filters by deviceId, status, or severity.",
                    ParametersSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            deviceId = new { type = "integer" },
                            status = new { type = "string", description = "Active, Acknowledged, or Resolved" },
                            severity = new { type = "string", description = "low, medium, high, or critical" }
                        }
                    }
                },
                new LlmToolDefinition
                {
                    Name = "get_sensors_by_device",
                    Description = "Get all sensors configured on a device.",
                    ParametersSchema = new
                    {
                        type = "object",
                        properties = new { deviceId = new { type = "integer" } },
                        required = new[] { "deviceId" }
                    }
                },
                new LlmToolDefinition
                {
                    Name = "get_actuators_by_device",
                    Description = "Get actuators on a device including LastKnownState (on/off or analog value) and LastStateAt.",
                    ParametersSchema = new
                    {
                        type = "object",
                        properties = new { deviceId = new { type = "integer" } },
                        required = new[] { "deviceId" }
                    }
                },
                new LlmToolDefinition
                {
                    Name = "get_recent_readings",
                    Description = "Get recent sensor readings for a device over the last N hours (default 24, max 168).",
                    ParametersSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            deviceId = new { type = "integer" },
                            hours = new { type = "integer", description = "Hours of history, 1-168" }
                        },
                        required = new[] { "deviceId" }
                    }
                },
                new LlmToolDefinition
                {
                    Name = "get_system_health",
                    Description = "Get API and MQTT pipeline health including connection status and message counts.",
                    ParametersSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
                },
                new LlmToolDefinition
                {
                    Name = "find_devices",
                    Description = "Search devices by name, location, type, or ID. Leave query empty to list all.",
                    ParametersSchema = new
                    {
                        type = "object",
                        properties = new { query = new { type = "string", description = "Search text" } }
                    }
                },
                new LlmToolDefinition
                {
                    Name = "find_actuators",
                    Description = "Search actuators on a device by name or ID.",
                    ParametersSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            deviceId = new { type = "integer" },
                            query = new { type = "string" }
                        },
                        required = new[] { "deviceId" }
                    }
                },
                new LlmToolDefinition
                {
                    Name = "get_alert_summary",
                    Description = "Summary of active alerts with counts by severity and recent items. Optional deviceId filter.",
                    ParametersSchema = new
                    {
                        type = "object",
                        properties = new { deviceId = new { type = "integer" } }
                    }
                },
                new LlmToolDefinition
                {
                    Name = "get_sensor_reading_summary",
                    Description = "Min/max/average/latest sensor readings for a device over the last N hours.",
                    ParametersSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            deviceId = new { type = "integer" },
                            hours = new { type = "integer", description = "1-168, default 24" },
                            sensorId = new { type = "integer" }
                        },
                        required = new[] { "deviceId" }
                    }
                },
                new LlmToolDefinition
                {
                    Name = "get_operational_snapshot",
                    Description = "Correlated MQTT health, offline devices, and active alert counts. Optional focus deviceId.",
                    ParametersSchema = new
                    {
                        type = "object",
                        properties = new { deviceId = new { type = "integer" } }
                    }
                },
                new LlmToolDefinition
                {
                    Name = "search_documentation",
                    Description = "Search project documentation (README, user manual, API docs, deployment guide) for setup, MQTT, troubleshooting, and how-to questions.",
                    ParametersSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            query = new { type = "string", description = "Search query for project docs" }
                        },
                        required = new[] { "query" }
                    }
                }
            };
    }
}
