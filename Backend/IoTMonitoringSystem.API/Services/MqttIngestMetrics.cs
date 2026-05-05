using System.Threading;

namespace IoTMonitoringSystem.API.Services
{
    public class MqttIngestMetrics : IMqttIngestMetrics
    {
        private long _totalMessagesReceived;
        private long _sensorReadingsPersisted;
        private long _sensorReadingPersistErrors;
        private long _commandAcksProcessed;
        private long _commandAckErrors;
        private long _unknownTopicMessages;
        private DateTime? _lastReadingPersistedAtUtc;
        private DateTime? _lastAckProcessedAtUtc;
        private readonly object _stateLock = new();

        public long TotalMessagesReceived => Interlocked.Read(ref _totalMessagesReceived);
        public long SensorReadingsPersisted => Interlocked.Read(ref _sensorReadingsPersisted);
        public long SensorReadingPersistErrors => Interlocked.Read(ref _sensorReadingPersistErrors);
        public long CommandAcksProcessed => Interlocked.Read(ref _commandAcksProcessed);
        public long CommandAckErrors => Interlocked.Read(ref _commandAckErrors);
        public long UnknownTopicMessages => Interlocked.Read(ref _unknownTopicMessages);
        public DateTime? LastReadingPersistedAtUtc { get { lock (_stateLock) return _lastReadingPersistedAtUtc; } }
        public DateTime? LastAckProcessedAtUtc { get { lock (_stateLock) return _lastAckProcessedAtUtc; } }

        public void IncrementMessagesReceived() => Interlocked.Increment(ref _totalMessagesReceived);

        public void IncrementSensorReadingsPersisted()
        {
            Interlocked.Increment(ref _sensorReadingsPersisted);
            lock (_stateLock)
            {
                _lastReadingPersistedAtUtc = DateTime.UtcNow;
            }
        }

        public void IncrementSensorReadingPersistErrors() => Interlocked.Increment(ref _sensorReadingPersistErrors);

        public void IncrementCommandAcksProcessed()
        {
            Interlocked.Increment(ref _commandAcksProcessed);
            lock (_stateLock)
            {
                _lastAckProcessedAtUtc = DateTime.UtcNow;
            }
        }

        public void IncrementCommandAckErrors() => Interlocked.Increment(ref _commandAckErrors);

        public void IncrementUnknownTopicMessages() => Interlocked.Increment(ref _unknownTopicMessages);
    }
}

