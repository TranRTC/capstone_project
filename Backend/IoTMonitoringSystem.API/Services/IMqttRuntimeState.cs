namespace IoTMonitoringSystem.API.Services
{
    public interface IMqttRuntimeState
    {
        bool IsConnected { get; }
        bool IsSubscribed { get; }
        DateTime? LastConnectAttemptAtUtc { get; }
        DateTime? LastConnectedAtUtc { get; }
        DateTime? LastMessageReceivedAtUtc { get; }
        int ReconnectAttempts { get; }
        string? LastError { get; }
    }
}

