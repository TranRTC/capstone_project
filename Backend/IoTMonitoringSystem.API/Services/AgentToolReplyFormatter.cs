using System.Text;
using System.Text.Json;

namespace IoTMonitoringSystem.API.Services
{
    public static class AgentToolReplyFormatter
    {
        public static string Format(string toolName, string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("error", out var err))
                    return $"I could not fetch that data: {err.GetString()}";

                return toolName switch
                {
                    "get_alert_summary" => FormatAlertSummary(doc.RootElement),
                    "get_operational_snapshot" => FormatOperationalSnapshot(doc.RootElement),
                    "get_system_health" => FormatSystemHealth(doc.RootElement),
                    "find_devices" => FormatFindDevices(doc.RootElement),
                    "get_devices" => FormatFindDevices(doc.RootElement),
                    "get_sensor_reading_summary" => FormatSensorSummary(doc.RootElement),
                    "get_active_alerts" => FormatActiveAlerts(doc.RootElement),
                    "get_actuators_by_device" => FormatActuators(doc.RootElement),
                    "find_actuators" => FormatActuators(doc.RootElement),
                    "get_sensors_by_device" => FormatSensors(doc.RootElement),
                    _ => FormatGeneric(json)
                };
            }
            catch
            {
                return FormatGeneric(json);
            }
        }

        private static string FormatAlertSummary(JsonElement root)
        {
            var sb = new StringBuilder();
            var total = root.TryGetProperty("totalActive", out var t) ? t.GetInt32() : 0;
            sb.AppendLine($"**Active alerts:** {total}");

            if (root.TryGetProperty("bySeverity", out var sev) && sev.ValueKind == JsonValueKind.Object)
            {
                foreach (var item in sev.EnumerateObject())
                    sb.AppendLine($"- {item.Name}: {item.Value.GetInt32()}");
            }

            if (root.TryGetProperty("recent", out var recent) && recent.ValueKind == JsonValueKind.Array)
            {
                sb.AppendLine();
                sb.AppendLine("Recent:");
                foreach (var alert in recent.EnumerateArray().Take(10))
                {
                    var id = alert.GetProperty("alertId").GetInt64();
                    var deviceId = alert.GetProperty("deviceId").GetInt32();
                    var severity = alert.GetProperty("severity").GetString();
                    var message = alert.GetProperty("message").GetString();
                    sb.AppendLine($"- Alert {id} (device {deviceId}, {severity}): {message}");
                }
            }

            return sb.ToString().Trim();
        }

        private static string FormatOperationalSnapshot(JsonElement root)
        {
            var sb = new StringBuilder("**Operational snapshot**\n");

            if (root.TryGetProperty("mqtt", out var mqtt))
            {
                var connected = mqtt.TryGetProperty("isConnected", out var c) && c.GetBoolean();
                sb.AppendLine($"- MQTT: {(connected ? "connected" : "disconnected")}");
                if (mqtt.TryGetProperty("lastError", out var err) && err.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(err.GetString()))
                    sb.AppendLine($"  Last error: {err.GetString()}");
            }

            if (root.TryGetProperty("devices", out var devices))
            {
                var offline = devices.TryGetProperty("offlineCount", out var oc) ? oc.GetInt32() : 0;
                var total = devices.TryGetProperty("total", out var tc) ? tc.GetInt32() : 0;
                sb.AppendLine($"- Devices: {total} total, {offline} offline (> threshold)");
            }

            if (root.TryGetProperty("alerts", out var alerts))
            {
                var active = alerts.TryGetProperty("activeCount", out var ac) ? ac.GetInt32() : 0;
                var critical = alerts.TryGetProperty("criticalCount", out var cc) ? cc.GetInt32() : 0;
                sb.AppendLine($"- Alerts: {active} active ({critical} critical)");
            }

            if (root.TryGetProperty("focusDevice", out var focus) && focus.ValueKind == JsonValueKind.Object)
            {
                if (focus.TryGetProperty("error", out _))
                {
                    sb.AppendLine($"- Focus device: {focus.GetProperty("error").GetString()}");
                }
                else
                {
                    var name = focus.TryGetProperty("deviceName", out var n) ? n.GetString() : "device";
                    var id = focus.TryGetProperty("deviceId", out var idProp) ? idProp.GetInt32() : 0;
                    var offline = focus.TryGetProperty("isOffline", out var off) && off.GetBoolean();
                    sb.AppendLine($"- Focus device {id} ({name}): {(offline ? "offline" : "online")}");
                }
            }

            return sb.ToString().Trim();
        }

        private static string FormatSystemHealth(JsonElement root)
        {
            if (root.TryGetProperty("mqtt", out var mqtt))
            {
                var connected = mqtt.TryGetProperty("isConnected", out var c) && c.GetBoolean();
                var messages = mqtt.TryGetProperty("totalMessagesReceived", out var m) ? m.GetInt64() : 0;
                return $"MQTT is {(connected ? "connected" : "disconnected")}. Total messages received: {messages}.";
            }
            return FormatGeneric(root.GetRawText());
        }

        private static string FormatFindDevices(JsonElement root)
        {
            if (!root.TryGetProperty("devices", out var devices) || devices.ValueKind != JsonValueKind.Array)
                return "No devices found.";

            var sb = new StringBuilder();
            foreach (var d in devices.EnumerateArray().Take(20))
            {
                var id = d.GetProperty("deviceId").GetInt32();
                var name = d.GetProperty("deviceName").GetString();
                var location = d.TryGetProperty("location", out var loc) ? loc.GetString() : null;
                sb.AppendLine($"- Device {id}: {name}{(string.IsNullOrWhiteSpace(location) ? "" : $" ({location})")}");
            }
            return sb.ToString().Trim();
        }

        private static string FormatSensorSummary(JsonElement root)
        {
            if (!root.TryGetProperty("sensors", out var sensors) || sensors.ValueKind != JsonValueKind.Array)
                return "No sensor readings in the selected period.";

            var sb = new StringBuilder();
            foreach (var s in sensors.EnumerateArray())
            {
                var name = s.GetProperty("sensorName").GetString();
                var latest = s.GetProperty("latestValue").GetDecimal();
                var unit = s.TryGetProperty("unit", out var u) ? u.GetString() : "";
                var avg = s.TryGetProperty("average", out var a) && a.ValueKind != JsonValueKind.Null ? a.GetDecimal() : (decimal?)null;
                sb.AppendLine($"- {name}: latest {latest}{unit}, avg {avg}{unit}");
            }
            return sb.ToString().Trim();
        }

        private static string FormatActiveAlerts(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Array)
                return FormatGeneric(root.GetRawText());

            var list = root.EnumerateArray().ToList();
            if (list.Count == 0)
                return "There are no active alerts.";

            var sb = new StringBuilder($"**{list.Count} active alert(s):**\n");
            foreach (var alert in list.Take(15))
            {
                var id = alert.GetProperty("alertId").GetInt64();
                var deviceId = alert.GetProperty("deviceId").GetInt32();
                var severity = alert.GetProperty("severity").GetString();
                var message = alert.GetProperty("message").GetString();
                sb.AppendLine($"- Alert {id} (device {deviceId}, {severity}): {message}");
            }
            return sb.ToString().Trim();
        }

        private static string FormatActuators(JsonElement root)
        {
            var actuators = root.ValueKind == JsonValueKind.Array
                ? root
                : root.TryGetProperty("actuators", out var nested) ? nested : default;

            if (actuators.ValueKind != JsonValueKind.Array)
                return "No actuators found for this device.";

            var list = actuators.EnumerateArray().ToList();
            if (list.Count == 0)
                return "No actuators found for this device.";

            var sb = new StringBuilder($"**{list.Count} actuator(s):**\n");
            foreach (var a in list)
            {
                var id = GetInt(a, "ActuatorId", "actuatorId");
                var deviceId = GetInt(a, "DeviceId", "deviceId");
                var name = GetString(a, "Name", "name") ?? $"Actuator {id}";
                var kind = GetString(a, "Kind", "kind");
                var state = GetString(a, "stateDescription", "stateDescription")
                    ?? GetString(a, "LastKnownState", "lastKnownState")
                    ?? "unknown";
                var isOn = TryGetBool(a, "parsedIsOn", "parsedIsOn");
                var toggleTo = TryGetBool(a, "toggleWouldSetOn", "toggleWouldSetOn");

                sb.AppendLine($"- Actuator {id} ({name}) on device {deviceId}");
                if (!string.IsNullOrWhiteSpace(kind))
                    sb.AppendLine($"  Kind: {kind}");
                sb.AppendLine($"  State: {state}{(isOn.HasValue ? $" (parsedIsOn: {isOn.Value})" : "")}");
                if (toggleTo.HasValue)
                    sb.AppendLine($"  Toggle would set: {(toggleTo.Value ? "on" : "off")}");
            }

            return sb.ToString().Trim();
        }

        private static string FormatSensors(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Array)
                return "No sensors found for this device.";

            var list = root.EnumerateArray().ToList();
            if (list.Count == 0)
                return "No sensors found for this device.";

            var sb = new StringBuilder($"**{list.Count} sensor(s):**\n");
            foreach (var s in list)
            {
                var id = GetInt(s, "SensorId", "sensorId");
                var name = GetString(s, "SensorName", "sensorName") ?? $"Sensor {id}";
                var type = GetString(s, "SensorType", "sensorType");
                var unit = GetString(s, "Unit", "unit");
                sb.AppendLine($"- Sensor {id}: {name}{(string.IsNullOrWhiteSpace(type) ? "" : $" ({type})")}{(string.IsNullOrWhiteSpace(unit) ? "" : $", {unit}")}");
            }

            return sb.ToString().Trim();
        }

        private static int GetInt(JsonElement el, string pascal, string camel)
        {
            if (el.TryGetProperty(pascal, out var v) && v.TryGetInt32(out var i)) return i;
            if (el.TryGetProperty(camel, out v) && v.TryGetInt32(out i)) return i;
            return 0;
        }

        private static string? GetString(JsonElement el, string pascal, string camel)
        {
            if (el.TryGetProperty(pascal, out var v) && v.ValueKind == JsonValueKind.String) return v.GetString();
            if (el.TryGetProperty(camel, out v) && v.ValueKind == JsonValueKind.String) return v.GetString();
            return null;
        }

        private static bool? TryGetBool(JsonElement el, string pascal, string camel)
        {
            if (el.TryGetProperty(pascal, out var v) && (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False))
                return v.GetBoolean();
            if (el.TryGetProperty(camel, out v) && (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False))
                return v.GetBoolean();
            return null;
        }

        private static string FormatGeneric(string json) =>
            json.Length > 1500 ? json[..1500] + "…" : json;
    }
}
