namespace IoTMonitoringSystem.API.Services
{
    public static class ActuatorStateHelper
    {
        public static bool? TryParseIsOn(string? lastKnownState)
        {
            if (string.IsNullOrWhiteSpace(lastKnownState))
                return null;

            var normalized = lastKnownState.Trim().ToLowerInvariant();
            return normalized switch
            {
                "on" or "true" or "1" => true,
                "off" or "false" or "0" => false,
                _ => decimal.TryParse(normalized, out var numeric) ? numeric != 0 : null
            };
        }

        /// <summary>Target power state when toggling a discrete actuator.</summary>
        public static bool ToggleTargetIsOn(string? lastKnownState) =>
            TryParseIsOn(lastKnownState) switch
            {
                true => false,
                false => true,
                null => true
            };

        public static string DescribeState(string? lastKnownState)
        {
            return TryParseIsOn(lastKnownState) switch
            {
                true => "on",
                false => "off",
                null => "unknown (treat as off for toggle)"
            };
        }
    }
}
