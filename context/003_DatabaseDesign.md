# Database Design Document

## Document Information
- **Project:** Web-Based IoT Device Real-Time Monitoring System
- **Version:** 1.0
- **Date:** 2026
- **Database:** SQL Server
- **Status:** Draft

## 1. Introduction

### 1.1 Purpose
This document describes the database design for the Web-Based IoT Device Real-Time Monitoring System. It defines the database schema, tables, relationships, constraints, and indexes required to support the system's functionality.

### 1.2 Scope
The database design covers:
- Device and sensor management
- Real-time sensor data storage
- Operational metrics storage
- Device status tracking
- Alert management
- Historical data storage for analysis

### 1.3 Design Principles
- **Normalization:** Database follows 3NF (Third Normal Form) to minimize redundancy
- **Performance:** Indexes on frequently queried columns (timestamps, device IDs)
- **Scalability:** Design supports high-volume time-series data
- **Data Integrity:** Foreign keys and constraints ensure data consistency
- **Extensibility:** Schema allows for future enhancements

## 2. Entity Relationship Diagram (ERD)

### 2.1 Entity Relationships Overview

```
Devices (1) ────< (Many) Sensors
Devices (1) ────< (Many) SensorReadings
Devices (1) ────< (Many) DeviceStatusHistory
Devices (1) ────< (Many) OperationalMetrics
Devices (1) ────< (Many) AlertRules
Devices (1) ────< (Many) DeviceConfigurations
Sensors (1) ────< (Many) SensorReadings
Sensors (1) ────< (Many) AlertRules (optional)
AlertRules (1) ────< (Many) Alerts
```

### 2.2 Key Relationships
- **Devices to Sensors:** One device can have multiple sensors
- **Devices to SensorReadings:** One device generates many sensor readings over time
- **Sensors to SensorReadings:** One sensor generates many readings
- **Devices to DeviceStatusHistory:** Device status changes are logged
- **Devices to OperationalMetrics:** Operational metrics are calculated per device
- **Devices to AlertRules:** Each device can have multiple alert rules
- **Sensors to AlertRules:** Sensors can have specific alert rules (optional)
- **AlertRules to Alerts:** Alert rules generate alert records when triggered
- **Devices to DeviceConfigurations:** Devices have configuration settings

## 3. Database Tables

### 3.0 Table Summary

The database consists of **8 tables** organized as follows:

1. **Devices** (3.1) - Core device/equipment information
2. **Sensors** (3.2) - Sensor definitions and configurations
3. **SensorReadings** (3.3) - Time-series sensor data (high volume)
4. **DeviceStatusHistory** (3.4) - Device status change tracking
5. **OperationalMetrics** (3.5) - Calculated operational metrics
6. **AlertRules** (3.6) - Configurable alert rule definitions
7. **Alerts** (3.7) - Alert records when rules are triggered
8. **DeviceConfigurations** (3.8) - Device configuration settings

**Table Relationships:**
- **Devices** is the central table (most relationships start here)
- **SensorReadings** is the highest volume table (time-series data)
- **Alerts** are generated from **AlertRules** when conditions are met

---

### 3.1 Devices Table

**Purpose:** Stores information about IoT devices (equipment/device) being monitored.

| Column Name | Data Type | Constraints | Description |
|------------|-----------|-------------|-------------|
| DeviceId | INT | PRIMARY KEY, IDENTITY(1,1) | Unique identifier for device |
| DeviceName | NVARCHAR(100) | NOT NULL | Display name of the device |
| DeviceType | NVARCHAR(50) | NOT NULL | Type of device (Pump, Motor, HVAC, etc.) |
| Location | NVARCHAR(200) | NULL | Physical location of device |
| FacilityType | NVARCHAR(50) | NULL | Facility type (Industrial, Commercial, Agricultural) |
| EdgeDeviceType | NVARCHAR(50) | NULL | Edge device type (Arduino, ESP32, ESP8266, Raspberry Pi, Modbus Gateway, etc.) |
| EdgeDeviceId | NVARCHAR(100) | NULL, UNIQUE | Unique identifier from edge device (e.g., "ESP32-001", "RPI-002") |
| IsActive | BIT | NOT NULL, DEFAULT 1 | Whether device is active |
| CreatedAt | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | Record creation timestamp |
| UpdatedAt | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | Last update timestamp |
| LastSeenAt | DATETIME2 | NULL | Last time device sent data (connection status) |
| Description | NVARCHAR(500) | NULL | Additional device description |

**Indexes:**
- PRIMARY KEY: DeviceId
- UNIQUE INDEX: EdgeDeviceId (where not null) - ensures unique edge device identifiers
- INDEX: DeviceType, IsActive (for filtering)
- INDEX: LastSeenAt (for connection status queries)

**Notes:**
- `LastSeenAt` is used to determine if device is online/offline
- `EdgeDeviceId` is the unique identifier from the physical edge device (e.g., "ESP32-001")
- `EdgeDeviceType` indicates the type of edge device hardware
- UNIQUE constraint on EdgeDeviceId ensures each edge device identifier is distinct

---

### 3.2 Sensors Table

**Purpose:** Stores information about sensors connected to devices.

| Column Name | Data Type | Constraints | Description |
|------------|-----------|-------------|-------------|
| SensorId | INT | PRIMARY KEY, IDENTITY(1,1) | Unique identifier for sensor |
| DeviceId | INT | NOT NULL, FOREIGN KEY | Reference to Devices table |
| EdgeDeviceId | NVARCHAR(100) | NULL | Identifier of edge device reading this sensor (if different from device's edge device) |
| SensorName | NVARCHAR(100) | NOT NULL | Name of the sensor |
| SensorType | NVARCHAR(50) | NOT NULL | Type of sensor (Temperature, Pressure, Vibration, Current, etc.) |
| Unit | NVARCHAR(20) | NULL | Unit of measurement (°C, PSI, RPM, A, etc.) |
| MinValue | DECIMAL(18,2) | NULL | Minimum expected value |
| MaxValue | DECIMAL(18,2) | NULL | Maximum expected value |
| IsActive | BIT | NOT NULL, DEFAULT 1 | Whether sensor is active |
| CreatedAt | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | Record creation timestamp |
| UpdatedAt | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | Last update timestamp |

**Indexes:**
- PRIMARY KEY: SensorId
- FOREIGN KEY: DeviceId → Devices(DeviceId)
- INDEX: DeviceId, SensorType (for filtering)
- INDEX: EdgeDeviceId (for edge device queries)

**Notes:**
- Sensors are associated with devices
- EdgeDeviceId is optional - if NULL, sensor uses device's edge device; if set, sensor has its own edge device (e.g., Modbus TCP sensor)
- MinValue/MaxValue can be used for validation and alerts

---

### 3.3 SensorReadings Table

**Purpose:** Stores time-series sensor data readings from edge devices.

| Column Name | Data Type | Constraints | Description |
|------------|-----------|-------------|-------------|
| ReadingId | BIGINT | PRIMARY KEY, IDENTITY(1,1) | Unique identifier for reading |
| DeviceId | INT | NOT NULL, FOREIGN KEY | Reference to Devices table |
| SensorId | INT | NOT NULL, FOREIGN KEY | Reference to Sensors table |
| Value | DECIMAL(18,4) | NOT NULL | Sensor reading value |
| Timestamp | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | When reading was taken |
| Status | NVARCHAR(20) | NULL | Status indicator (Good, Warning, Critical, Failure) |
| Quality | NVARCHAR(20) | NULL | Data quality indicator |
| CreatedAt | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | Record creation timestamp |

**Indexes:**
- PRIMARY KEY: ReadingId
- FOREIGN KEY: DeviceId → Devices(DeviceId)
- FOREIGN KEY: SensorId → Sensors(SensorId)
- INDEX: Timestamp, DeviceId (for time-series queries)
- INDEX: DeviceId, SensorId, Timestamp (for device/sensor history)
- INDEX: Status (for filtering by status)

**Notes:**
- This table will have high write volume (time-series data)
- Consider partitioning by date for large datasets
- `Status` field can be calculated based on thresholds or set by edge device
- `Timestamp` should be indexed for efficient historical queries

---

### 3.4 DeviceStatusHistory Table

**Purpose:** Tracks device status changes over time (online/offline, running/stopped, etc.).

| Column Name | Data Type | Constraints | Description |
|------------|-----------|-------------|-------------|
| StatusHistoryId | BIGINT | PRIMARY KEY, IDENTITY(1,1) | Unique identifier |
| DeviceId | INT | NOT NULL, FOREIGN KEY | Reference to Devices table |
| Status | NVARCHAR(50) | NOT NULL | Status value (Online, Offline, Running, Stopped, Error, etc.) |
| PreviousStatus | NVARCHAR(50) | NULL | Previous status before change |
| StatusCode | INT | NULL | Numeric status code |
| Message | NVARCHAR(500) | NULL | Status change message or description |
| Timestamp | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | When status changed |
| CreatedAt | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | Record creation timestamp |

**Indexes:**
- PRIMARY KEY: StatusHistoryId
- FOREIGN KEY: DeviceId → Devices(DeviceId)
- INDEX: DeviceId, Timestamp (for device status history)
- INDEX: Status, Timestamp (for status-based queries)

**Notes:**
- Tracks all status changes for trace-back analysis
- Used to determine device connection status and operational state

---

### 3.5 OperationalMetrics Table

**Purpose:** Stores calculated operational metrics (efficiency, throughput, utilization, etc.).

| Column Name | Data Type | Constraints | Description |
|------------|-----------|-------------|-------------|
| MetricId | BIGINT | PRIMARY KEY, IDENTITY(1,1) | Unique identifier |
| DeviceId | INT | NOT NULL, FOREIGN KEY | Reference to Devices table |
| MetricType | NVARCHAR(50) | NOT NULL | Type of metric (Efficiency, Throughput, Utilization, Uptime, etc.) |
| MetricValue | DECIMAL(18,4) | NOT NULL | Calculated metric value |
| Unit | NVARCHAR(20) | NULL | Unit of measurement (%, units/hour, %, hours, etc.) |
| CalculationPeriod | NVARCHAR(20) | NULL | Period for calculation (Hour, Day, Week, etc.) |
| PeriodStart | DATETIME2 | NULL | Start of calculation period |
| PeriodEnd | DATETIME2 | NULL | End of calculation period |
| Timestamp | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | When metric was calculated |
| CreatedAt | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | Record creation timestamp |

**Indexes:**
- PRIMARY KEY: MetricId
- FOREIGN KEY: DeviceId → Devices(DeviceId)
- INDEX: DeviceId, MetricType, Timestamp (for metric history)
- INDEX: Timestamp (for time-based queries)

**Notes:**
- Metrics are calculated from sensor data
- Can be aggregated over different time periods
- Supports performance analysis and decision-making

---

### 3.6 AlertRules Table

**Purpose:** Stores configurable alert rules for devices and sensors.

| Column Name | Data Type | Constraints | Description |
|------------|-----------|-------------|-------------|
| AlertRuleId | INT | PRIMARY KEY, IDENTITY(1,1) | Unique identifier |
| DeviceId | INT | NULL, FOREIGN KEY | Reference to Devices table (null for global rules) |
| SensorId | INT | NULL, FOREIGN KEY | Reference to Sensors table (null for device-level rules) |
| RuleName | NVARCHAR(100) | NOT NULL | Name of the alert rule |
| RuleType | NVARCHAR(50) | NOT NULL | Type of rule (Threshold, StatusChange, Anomaly, etc.) |
| Condition | NVARCHAR(200) | NOT NULL | Condition expression (e.g., "Value > 100", "Status = 'Failure'") |
| ThresholdValue | DECIMAL(18,4) | NULL | Threshold value for comparison |
| ComparisonOperator | NVARCHAR(10) | NULL | Operator (>, <, >=, <=, =, !=) |
| Severity | NVARCHAR(20) | NOT NULL | Alert severity (Info, Warning, Critical) |
| IsEnabled | BIT | NOT NULL, DEFAULT 1 | Whether rule is active |
| CreatedAt | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | Record creation timestamp |
| UpdatedAt | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | Last update timestamp |

**Indexes:**
- PRIMARY KEY: AlertRuleId
- FOREIGN KEY: DeviceId → Devices(DeviceId)
- FOREIGN KEY: SensorId → Sensors(SensorId)
- INDEX: DeviceId, IsEnabled (for active rules per device)
- INDEX: IsEnabled (for filtering active rules)

**Notes:**
- Rules can be device-specific, sensor-specific, or global
- Supports threshold-based and status-based alerts

---

### 3.7 Alerts Table

**Purpose:** Stores alert records when alert rules are triggered.

| Column Name | Data Type | Constraints | Description |
|------------|-----------|-------------|-------------|
| AlertId | BIGINT | PRIMARY KEY, IDENTITY(1,1) | Unique identifier |
| AlertRuleId | INT | NOT NULL, FOREIGN KEY | Reference to AlertRules table |
| DeviceId | INT | NOT NULL, FOREIGN KEY | Reference to Devices table |
| SensorId | INT | NULL, FOREIGN KEY | Reference to Sensors table (if sensor-specific) |
| Severity | NVARCHAR(20) | NOT NULL | Alert severity (Info, Warning, Critical) |
| Message | NVARCHAR(500) | NOT NULL | Alert message |
| TriggerValue | DECIMAL(18,4) | NULL | Value that triggered the alert |
| Status | NVARCHAR(20) | NOT NULL, DEFAULT 'Active' | Alert status (Active, Acknowledged, Resolved) |
| TriggeredAt | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | When alert was triggered |
| AcknowledgedAt | DATETIME2 | NULL | When alert was acknowledged |
| ResolvedAt | DATETIME2 | NULL | When alert was resolved |
| CreatedAt | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | Record creation timestamp |

**Indexes:**
- PRIMARY KEY: AlertId
- FOREIGN KEY: AlertRuleId → AlertRules(AlertRuleId)
- FOREIGN KEY: DeviceId → Devices(DeviceId)
- FOREIGN KEY: SensorId → Sensors(SensorId)
- INDEX: DeviceId, Status, TriggeredAt (for active alerts per device)
- INDEX: Status, TriggeredAt (for filtering active alerts)
- INDEX: TriggeredAt (for time-based queries)

**Notes:**
- Alerts are created when rules are triggered
- Supports alert lifecycle (Active → Acknowledged → Resolved)
- Used for notification system and alert history

---

### 3.8 DeviceConfigurations Table

**Purpose:** Stores device configuration settings and parameters.

| Column Name | Data Type | Constraints | Description |
|------------|-----------|-------------|-------------|
| ConfigurationId | INT | PRIMARY KEY, IDENTITY(1,1) | Unique identifier |
| DeviceId | INT | NOT NULL, FOREIGN KEY | Reference to Devices table |
| ConfigurationKey | NVARCHAR(100) | NOT NULL | Configuration parameter name |
| ConfigurationValue | NVARCHAR(500) | NULL | Configuration parameter value |
| ValueType | NVARCHAR(20) | NULL | Value type (String, Number, Boolean, JSON) |
| Description | NVARCHAR(500) | NULL | Description of the configuration |
| CreatedAt | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | Record creation timestamp |
| UpdatedAt | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | Last update timestamp |

**Indexes:**
- PRIMARY KEY: ConfigurationId
- FOREIGN KEY: DeviceId → Devices(DeviceId)
- UNIQUE INDEX: DeviceId, ConfigurationKey (one value per key per device)
- INDEX: DeviceId (for device configuration queries)

**Notes:**
- Flexible key-value storage for device settings
- Supports various configuration types
- Can store JSON for complex configurations

---

## 4. Database Relationships

### 4.1 Primary Relationships

1. **Devices → Sensors** (One-to-Many)
   - One device can have multiple sensors
   - Cascade delete: If device is deleted, sensors are deleted

2. **Devices → SensorReadings** (One-to-Many)
   - One device generates many sensor readings
   - Cascade delete: If device is deleted, readings are deleted

3. **Sensors → SensorReadings** (One-to-Many)
   - One sensor generates many readings
   - Cascade delete: If sensor is deleted, readings are deleted

4. **Devices → DeviceStatusHistory** (One-to-Many)
   - Device status changes are logged
   - Cascade delete: If device is deleted, status history is deleted

5. **Devices → OperationalMetrics** (One-to-Many)
   - Metrics are calculated per device
   - Cascade delete: If device is deleted, metrics are deleted

6. **Devices → AlertRules** (One-to-Many, Optional)
   - Devices can have multiple alert rules
   - Cascade delete: If device is deleted, rules are deleted

7. **Sensors → AlertRules** (One-to-Many, Optional)
   - Sensors can have specific alert rules
   - Cascade delete: If sensor is deleted, rules are deleted

8. **AlertRules → Alerts** (One-to-Many)
   - Alert rules generate alert records
   - Cascade delete: If rule is deleted, alerts are deleted

9. **Devices → DeviceConfigurations** (One-to-Many)
   - Devices have configuration settings
   - Cascade delete: If device is deleted, configurations are deleted

### 4.2 Referential Integrity

All foreign key relationships enforce referential integrity:
- **ON DELETE CASCADE:** Child records are deleted when parent is deleted
- **ON UPDATE CASCADE:** Foreign key values are updated when parent key changes

## 5. Indexes and Performance

### 5.1 Primary Indexes
- All tables have PRIMARY KEY indexes on identity columns
- Ensures fast lookups by primary key

### 5.2 Foreign Key Indexes
- All foreign key columns are indexed
- Improves join performance and referential integrity checks

### 5.3 Query Optimization Indexes

**Time-Series Queries (SensorReadings):**
- `Timestamp, DeviceId` - For time-range queries per device
- `DeviceId, SensorId, Timestamp` - For sensor history queries

**Status Queries:**
- `DeviceId, Timestamp` - For device status history
- `Status, Timestamp` - For status-based filtering

**Alert Queries:**
- `DeviceId, Status, TriggeredAt` - For active alerts per device
- `Status, TriggeredAt` - For alert filtering

**Device Queries:**
- `DeviceType, IsActive` - For device filtering
- `LastSeenAt` - For connection status determination

### 5.4 Performance Considerations

1. **Time-Series Data:**
   - SensorReadings table will grow large over time
   - Consider partitioning by date for large datasets
   - Implement data retention policies (archive old data)

2. **High Write Volume:**
   - SensorReadings table has high write frequency
   - Indexes should be optimized for write performance
   - Consider batch inserts for efficiency

3. **Query Patterns:**
   - Most queries filter by DeviceId and Timestamp
   - Indexes are designed to support these patterns
   - Consider covering indexes for frequently accessed columns

## 6. Data Types and Constraints

### 6.1 Data Type Choices

- **INT/BIGINT:** For IDs and counters (BIGINT for high-volume tables)
- **NVARCHAR:** For text data (supports Unicode)
- **DECIMAL(18,4):** For precise numeric values (sensor readings, metrics)
- **DATETIME2:** For timestamps (better precision than DATETIME)
- **BIT:** For boolean flags
- **DEFAULT GETUTCDATE():** For automatic timestamp management

### 6.2 Constraints

- **NOT NULL:** Required fields
- **DEFAULT:** Default values for optional fields
- **UNIQUE:** Unique constraints where needed
- **CHECK:** Can be added for value range validation
- **FOREIGN KEY:** Referential integrity

## 7. Data Retention and Archiving

### 7.1 Retention Strategy

**SensorReadings:**
- Keep detailed data for last 30 days
- Archive older data to separate table or file storage
- Keep aggregated data (hourly/daily averages) for longer periods

**DeviceStatusHistory:**
- Keep all status changes (relatively small volume)
- Can be retained indefinitely or archived after 1 year

**Alerts:**
- Keep active alerts indefinitely
- Archive resolved alerts after 90 days

**OperationalMetrics:**
- Keep metrics for analysis period (suggest 1 year minimum)

### 7.2 Archiving Approach

For capstone project:
- Focus on current functionality
- Data retention can be implemented as future enhancement
- Document retention strategy for production deployment

## 8. Entity Framework Core Mapping

### 8.1 Entity Classes

The database design maps to the following C# entity classes:

- `Device` → Devices table
- `Sensor` → Sensors table
- `SensorReading` → SensorReadings table
- `DeviceStatusHistory` → DeviceStatusHistory table
- `OperationalMetric` → OperationalMetrics table
- `AlertRule` → AlertRules table
- `Alert` → Alerts table
- `DeviceConfiguration` → DeviceConfigurations table

### 8.2 Navigation Properties

Entity Framework will use navigation properties for relationships:
- `Device.Sensors` - Collection of sensors
- `Device.SensorReadings` - Collection of readings
- `Sensor.Device` - Reference to parent device
- `Sensor.SensorReadings` - Collection of readings
- `AlertRule.Device` - Reference to device
- `Alert.Device` - Reference to device

## 9. Database Initialization

### 9.1 Initial Setup

1. Create database: `IoTMonitoringDB`
2. Run Entity Framework migrations to create tables
3. Seed initial data (if needed):
   - Sample device types
   - Default alert rule templates
   - Configuration defaults

### 9.2 Migration Strategy

- Use Entity Framework Core Migrations
- Version control all schema changes
- Document migration scripts
- Test migrations on development database first

## 10. Backup and Recovery

### 10.1 Backup Strategy

For capstone project:
- Regular database backups (daily recommended)
- Backup before major changes
- Store backups in safe location

### 10.2 Recovery Procedures

- Document recovery procedures
- Test backup restoration
- Keep backup copies of schema scripts

## 11. Future Enhancements

Potential database enhancements for future iterations:
- **Partitioning:** Partition SensorReadings by date for better performance
- **Materialized Views:** Pre-aggregated data for faster reporting
- **Full-Text Search:** For searching device descriptions and messages
- **Audit Tables:** Track all data changes for compliance
- **User Management:** If authentication is added later
- **Multi-tenancy:** Support for multiple organizations/facilities

## 12. Notes

- This design is optimized for capstone project scope
- Can be extended for production deployment
- Performance tuning may be needed based on actual usage patterns
- Consider database maintenance tasks (index rebuilding, statistics updates)

---

## Approval

- **Prepared by:** [Your Name]
- **Reviewed by:** [Reviewer Name]
- **Approved by:** [Approver Name]
- **Date:** [Date]

---

## Notes
This document is a living document and will be updated as the database design evolves during development.

