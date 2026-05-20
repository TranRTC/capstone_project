namespace IoTMonitoringSystem.Core.Entities
{
    /// <summary>How sensor readings should be interpreted in the UI.</summary>
    public enum SensorSignalKind
    {
        Analog = 0,
        Discrete = 1,
    }

    /// <summary>Visualization for analog sensors (ignored when SignalKind is Discrete).</summary>
    public enum SensorChartStyle
    {
        Line = 0,
        Gauge = 1,
    }
}
