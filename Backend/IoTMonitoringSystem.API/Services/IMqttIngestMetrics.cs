namespace IoTMonitoringSystem.API.Services
{
    public interface IMqttIngestMetrics
    {
        long TotalMessagesReceived { get; }
        long SensorReadingsPersisted { get; }
        long SensorReadingPersistErrors { get; }
        long CommandAcksProcessed { get; }
        long CommandAckErrors { get; }
        long UnknownTopicMessages { get; }
        DateTime? LastReadingPersistedAtUtc { get; }
        DateTime? LastAckProcessedAtUtc { get; }

        void IncrementMessagesReceived();
        void IncrementSensorReadingsPersisted();
        void IncrementSensorReadingPersistErrors();
        void IncrementCommandAcksProcessed();
        void IncrementCommandAckErrors();
        void IncrementUnknownTopicMessages();
    }
}

