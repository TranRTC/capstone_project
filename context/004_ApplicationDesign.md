# Application Design Document

## Document Information
- **Project:** Web-Based IoT Device Real-Time Monitoring System
- **Version:** 1.0
- **Date:** 2026
- **Status:** Draft

## 1. Introduction

### 1.1 Purpose
This document describes the application architecture and design for the Web-Based IoT Device Real-Time Monitoring System. It defines the backend structure, API design, entity classes, service layers, and real-time communication architecture using ASP.NET Core Web API and SignalR.

### 1.2 Scope
The application design covers:
- Backend architecture and layers
- C# entity classes (matching database schema)
- RESTful API endpoints
- SignalR hub for real-time communication
- Service layer design
- Data access layer (Entity Framework Core)
- Data Transfer Objects (DTOs)
- Error handling and validation

### 1.3 Technology Stack
- **Framework:** ASP.NET Core 8.0
- **ORM:** Entity Framework Core
- **Real-Time:** SignalR
- **Database:** SQL Server
- **Language:** C#
- **IDE:** Visual Studio 2022 / Visual Studio Code

## 2. Application Architecture

### 2.1 Layered Architecture

The application follows a **layered architecture** pattern:

```
┌─────────────────────────────────────┐
│         Presentation Layer           │
│    (Controllers, SignalR Hub)        │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│         Service Layer               │
│    (Business Logic, Validation)     │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│      Data Access Layer              │
│  (Entity Framework Core, Repos)    │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│         Database Layer              │
│         (SQL Server)                 │
└─────────────────────────────────────┘
```

### 2.2 Project Structure

```
IoTMonitoringSystem/
├── IoTMonitoringSystem.API/          # Web API project
│   ├── Controllers/                  # REST API controllers
│   ├── Hubs/                         # SignalR hubs
│   ├── Middleware/                   # Custom middleware
│   └── Program.cs                    # Application entry point
├── IoTMonitoringSystem.Core/         # Core domain models
│   ├── Entities/                     # Entity classes
│   ├── DTOs/                         # Data Transfer Objects
│   └── Enums/                        # Enumerations
├── IoTMonitoringSystem.Infrastructure/ # Data access
│   ├── Data/                         # DbContext
│   ├── Repositories/                 # Repository pattern
│   └── Migrations/                   # EF Core migrations
├── IoTMonitoringSystem.Services/     # Business logic
│   ├── DeviceService.cs
│   ├── SensorService.cs
│   ├── AlertService.cs
│   └── MetricsService.cs
└── IoTMonitoringSystem.Shared/       # Shared utilities
    ├── Helpers/
    └── Constants/
```

## 3. Entity Classes

### 3.1 Overview

Entity classes map directly to database tables defined in the Database Design Document (003_DatabaseDesign.md). These classes are used by Entity Framework Core for database operations.

### 3.2 Device Entity

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IoTMonitoringSystem.Core.Entities
{
    public class Device
    {
        [Key]
        public int DeviceId { get; set; }

        [Required]
        [MaxLength(100)]
        public string DeviceName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string DeviceType { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Location { get; set; }

        [MaxLength(50)]
        public string? FacilityType { get; set; }

        [MaxLength(50)]
        public string? EdgeDeviceType { get; set; }

        [MaxLength(100)]
        public string? EdgeDeviceId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastSeenAt { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        // Navigation Properties
        public ICollection<Sensor> Sensors { get; set; } = new List<Sensor>();
        public ICollection<SensorReading> SensorReadings { get; set; } = new List<SensorReading>();
        public ICollection<DeviceStatusHistory> DeviceStatusHistories { get; set; } = new List<DeviceStatusHistory>();
        public ICollection<OperationalMetric> OperationalMetrics { get; set; } = new List<OperationalMetric>();
        public ICollection<AlertRule> AlertRules { get; set; } = new List<AlertRule>();
        public ICollection<DeviceConfiguration> DeviceConfigurations { get; set; } = new List<DeviceConfiguration>();
    }
}
```

### 3.3 Sensor Entity

```csharp
namespace IoTMonitoringSystem.Core.Entities
{
    public class Sensor
    {
        [Key]
        public int SensorId { get; set; }

        [Required]
        public int DeviceId { get; set; }

        [MaxLength(100)]
        public string? EdgeDeviceId { get; set; }

        [Required]
        [MaxLength(100)]
        public string SensorName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string SensorType { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Unit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxValue { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("DeviceId")]
        public Device Device { get; set; } = null!;

        public ICollection<SensorReading> SensorReadings { get; set; } = new List<SensorReading>();
        public ICollection<AlertRule> AlertRules { get; set; } = new List<AlertRule>();
    }
}
```

### 3.4 SensorReading Entity

```csharp
namespace IoTMonitoringSystem.Core.Entities
{
    public class SensorReading
    {
        [Key]
        public long ReadingId { get; set; }

        [Required]
        public int DeviceId { get; set; }

        [Required]
        public int SensorId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,4)")]
        public decimal Value { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(20)]
        public string? Status { get; set; }

        [MaxLength(20)]
        public string? Quality { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("DeviceId")]
        public Device Device { get; set; } = null!;

        [ForeignKey("SensorId")]
        public Sensor Sensor { get; set; } = null!;
    }
}
```

### 3.5 DeviceStatusHistory Entity

```csharp
namespace IoTMonitoringSystem.Core.Entities
{
    public class DeviceStatusHistory
    {
        [Key]
        public long StatusHistoryId { get; set; }

        [Required]
        public int DeviceId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? PreviousStatus { get; set; }

        public int? StatusCode { get; set; }

        [MaxLength(500)]
        public string? Message { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("DeviceId")]
        public Device Device { get; set; } = null!;
    }
}
```

### 3.6 OperationalMetric Entity

```csharp
namespace IoTMonitoringSystem.Core.Entities
{
    public class OperationalMetric
    {
        [Key]
        public long MetricId { get; set; }

        [Required]
        public int DeviceId { get; set; }

        [Required]
        [MaxLength(50)]
        public string MetricType { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,4)")]
        public decimal MetricValue { get; set; }

        [MaxLength(20)]
        public string? Unit { get; set; }

        [MaxLength(20)]
        public string? CalculationPeriod { get; set; }

        public DateTime? PeriodStart { get; set; }

        public DateTime? PeriodEnd { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("DeviceId")]
        public Device Device { get; set; } = null!;
    }
}
```

### 3.7 AlertRule Entity

```csharp
namespace IoTMonitoringSystem.Core.Entities
{
    public class AlertRule
    {
        [Key]
        public int AlertRuleId { get; set; }

        public int? DeviceId { get; set; }

        public int? SensorId { get; set; }

        [Required]
        [MaxLength(100)]
        public string RuleName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string RuleType { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Condition { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,4)")]
        public decimal? ThresholdValue { get; set; }

        [MaxLength(10)]
        public string? ComparisonOperator { get; set; }

        [Required]
        [MaxLength(20)]
        public string Severity { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("DeviceId")]
        public Device? Device { get; set; }

        [ForeignKey("SensorId")]
        public Sensor? Sensor { get; set; }

        public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    }
}
```

### 3.8 Alert Entity

```csharp
namespace IoTMonitoringSystem.Core.Entities
{
    public class Alert
    {
        [Key]
        public long AlertId { get; set; }

        [Required]
        public int AlertRuleId { get; set; }

        [Required]
        public int DeviceId { get; set; }

        public int? SensorId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Severity { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,4)")]
        public decimal? TriggerValue { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        [Required]
        public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;

        public DateTime? AcknowledgedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("AlertRuleId")]
        public AlertRule AlertRule { get; set; } = null!;

        [ForeignKey("DeviceId")]
        public Device Device { get; set; } = null!;

        [ForeignKey("SensorId")]
        public Sensor? Sensor { get; set; }
    }
}
```

### 3.9 DeviceConfiguration Entity

```csharp
namespace IoTMonitoringSystem.Core.Entities
{
    public class DeviceConfiguration
    {
        [Key]
        public int ConfigurationId { get; set; }

        [Required]
        public int DeviceId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ConfigurationKey { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ConfigurationValue { get; set; }

        [MaxLength(20)]
        public string? ValueType { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("DeviceId")]
        public Device Device { get; set; } = null!;
    }
}
```

## 4. Data Transfer Objects (DTOs)

### 4.1 Purpose

DTOs are used to transfer data between layers and to/from the API, avoiding direct exposure of entity classes.

### 4.2 Device DTOs

```csharp
namespace IoTMonitoringSystem.Core.DTOs
{
    // Request DTOs
    public class CreateDeviceDto
    {
        [Required]
        [MaxLength(100)]
        public string DeviceName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string DeviceType { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Location { get; set; }

        [MaxLength(50)]
        public string? FacilityType { get; set; }

        [MaxLength(50)]
        public string? EdgeDeviceType { get; set; }

        [MaxLength(100)]
        public string? EdgeDeviceId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public class UpdateDeviceDto
    {
        [MaxLength(100)]
        public string? DeviceName { get; set; }

        [MaxLength(50)]
        public string? DeviceType { get; set; }

        [MaxLength(200)]
        public string? Location { get; set; }

        [MaxLength(50)]
        public string? FacilityType { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool? IsActive { get; set; }
    }

    // Response DTOs
    public class DeviceDto
    {
        public int DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? FacilityType { get; set; }
        public string? EdgeDeviceType { get; set; }
        public string? EdgeDeviceId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public string? Description { get; set; }
    }

    public class DeviceListDto
    {
        public int DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string? Location { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastSeenAt { get; set; }
    }
}
```

### 4.3 SensorReading DTOs

```csharp
namespace IoTMonitoringSystem.Core.DTOs
{
    public class CreateSensorReadingDto
    {
        [Required]
        public int DeviceId { get; set; }

        [Required]
        public int SensorId { get; set; }

        [Required]
        public decimal Value { get; set; }

        public DateTime? Timestamp { get; set; }

        [MaxLength(20)]
        public string? Status { get; set; }

        [MaxLength(20)]
        public string? Quality { get; set; }
    }

    public class SensorReadingDto
    {
        public long ReadingId { get; set; }
        public int DeviceId { get; set; }
        public int SensorId { get; set; }
        public decimal Value { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Status { get; set; }
        public string? Quality { get; set; }
    }

    public class SensorReadingQueryDto
    {
        public int? DeviceId { get; set; }
        public int? SensorId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 100;
    }
}
```

## 5. RESTful API Design

### 5.1 API Base URL

```
https://api.iotmonitoring.com/api/v1
```

### 5.2 Device Management Endpoints

| Method | Endpoint | Description | Request Body | Response |
|--------|----------|-------------|--------------|----------|
| GET | `/devices` | Get all devices | - | `List<DeviceListDto>` |
| GET | `/devices/{id}` | Get device by ID | - | `DeviceDto` |
| POST | `/devices` | Create new device | `CreateDeviceDto` | `DeviceDto` |
| PUT | `/devices/{id}` | Update device | `UpdateDeviceDto` | `DeviceDto` |
| DELETE | `/devices/{id}` | Delete device | - | `204 No Content` |
| GET | `/devices/{id}/status` | Get device status | - | `DeviceStatusDto` |

### 5.3 Sensor Management Endpoints

| Method | Endpoint | Description | Request Body | Response |
|--------|----------|-------------|--------------|----------|
| GET | `/devices/{deviceId}/sensors` | Get sensors for device | - | `List<SensorDto>` |
| GET | `/sensors/{id}` | Get sensor by ID | - | `SensorDto` |
| POST | `/devices/{deviceId}/sensors` | Create sensor | `CreateSensorDto` | `SensorDto` |
| PUT | `/sensors/{id}` | Update sensor | `UpdateSensorDto` | `SensorDto` |
| DELETE | `/sensors/{id}` | Delete sensor | - | `204 No Content` |

### 5.4 Sensor Readings Endpoints

| Method | Endpoint | Description | Request Body | Response |
|--------|----------|-------------|--------------|----------|
| POST | `/sensorreadings` | Create sensor reading | `CreateSensorReadingDto` | `SensorReadingDto` |
| POST | `/sensorreadings/batch` | Batch create readings | `List<CreateSensorReadingDto>` | `List<SensorReadingDto>` |
| GET | `/sensorreadings` | Query sensor readings | Query params | `PagedResult<SensorReadingDto>` |
| GET | `/devices/{deviceId}/readings` | Get readings for device | Query params | `PagedResult<SensorReadingDto>` |
| GET | `/sensors/{sensorId}/readings` | Get readings for sensor | Query params | `PagedResult<SensorReadingDto>` |

### 5.5 Alert Management Endpoints

| Method | Endpoint | Description | Request Body | Response |
|--------|----------|-------------|--------------|----------|
| GET | `/alerts` | Get all alerts | Query params | `PagedResult<AlertDto>` |
| GET | `/alerts/{id}` | Get alert by ID | - | `AlertDto` |
| GET | `/alerts/active` | Get active alerts | - | `List<AlertDto>` |
| PUT | `/alerts/{id}/acknowledge` | Acknowledge alert | - | `AlertDto` |
| PUT | `/alerts/{id}/resolve` | Resolve alert | - | `AlertDto` |
| GET | `/alertrules` | Get all alert rules | - | `List<AlertRuleDto>` |
| POST | `/alertrules` | Create alert rule | `CreateAlertRuleDto` | `AlertRuleDto` |
| PUT | `/alertrules/{id}` | Update alert rule | `UpdateAlertRuleDto` | `AlertRuleDto` |
| DELETE | `/alertrules/{id}` | Delete alert rule | - | `204 No Content` |

### 5.6 Response Format

All API responses follow a standard format:

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
}
```

### 5.7 Error Responses

Standard HTTP status codes:
- `200 OK` - Success
- `201 Created` - Resource created
- `204 No Content` - Success with no content
- `400 Bad Request` - Validation error
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server error

## 6. SignalR Hub Design

### 6.1 Hub Overview

SignalR hub provides real-time bidirectional communication between server and clients.

### 6.2 MonitoringHub

```csharp
using Microsoft.AspNetCore.SignalR;

namespace IoTMonitoringSystem.API.Hubs
{
    public class MonitoringHub : Hub
    {
        // Client-to-Server Methods
        public async Task SubscribeToDevice(int deviceId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"device_{deviceId}");
        }

        public async Task UnsubscribeFromDevice(int deviceId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"device_{deviceId}");
        }

        public async Task SubscribeToAllDevices()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "all_devices");
        }

        // Server-to-Client Methods (called from services)
        // These are invoked by the service layer, not directly by clients
    }
}
```

### 6.3 Real-Time Events

The hub broadcasts the following events:

| Event Name | Description | Payload |
|------------|-------------|---------|
| `SensorReadingReceived` | New sensor reading | `SensorReadingDto` |
| `DeviceStatusChanged` | Device status updated | `DeviceStatusDto` |
| `AlertTriggered` | New alert triggered | `AlertDto` |
| `AlertAcknowledged` | Alert acknowledged | `AlertDto` |
| `AlertResolved` | Alert resolved | `AlertDto` |

### 6.4 Hub Usage Example

```csharp
// In service layer
public class SensorService
{
    private readonly IHubContext<MonitoringHub> _hubContext;

    public async Task CreateReadingAsync(CreateSensorReadingDto dto)
    {
        // Save to database
        var reading = await _repository.CreateAsync(dto);

        // Broadcast to SignalR clients
        await _hubContext.Clients.Group($"device_{dto.DeviceId}")
            .SendAsync("SensorReadingReceived", reading);

        await _hubContext.Clients.Group("all_devices")
            .SendAsync("SensorReadingReceived", reading);
    }
}
```

## 7. Service Layer Design

### 7.1 Service Interface Pattern

Each service implements an interface for testability and dependency injection.

### 7.2 DeviceService

```csharp
namespace IoTMonitoringSystem.Services
{
    public interface IDeviceService
    {
        Task<DeviceDto> GetDeviceByIdAsync(int deviceId);
        Task<List<DeviceListDto>> GetAllDevicesAsync();
        Task<DeviceDto> CreateDeviceAsync(CreateDeviceDto dto);
        Task<DeviceDto> UpdateDeviceAsync(int deviceId, UpdateDeviceDto dto);
        Task DeleteDeviceAsync(int deviceId);
        Task UpdateDeviceStatusAsync(int deviceId, string status, string? message = null);
    }

    public class DeviceService : IDeviceService
    {
        private readonly IDeviceRepository _repository;
        private readonly IHubContext<MonitoringHub> _hubContext;
        private readonly ILogger<DeviceService> _logger;

        // Implementation...
    }
}
```

### 7.3 SensorService

```csharp
namespace IoTMonitoringSystem.Services
{
    public interface ISensorService
    {
        Task<SensorDto> GetSensorByIdAsync(int sensorId);
        Task<List<SensorDto>> GetSensorsByDeviceIdAsync(int deviceId);
        Task<SensorDto> CreateSensorAsync(int deviceId, CreateSensorDto dto);
        Task<SensorDto> UpdateSensorAsync(int sensorId, UpdateSensorDto dto);
        Task DeleteSensorAsync(int sensorId);
    }
}
```

### 7.4 SensorReadingService

```csharp
namespace IoTMonitoringSystem.Services
{
    public interface ISensorReadingService
    {
        Task<SensorReadingDto> CreateReadingAsync(CreateSensorReadingDto dto);
        Task<List<SensorReadingDto>> CreateReadingsBatchAsync(List<CreateSensorReadingDto> dtos);
        Task<PagedResult<SensorReadingDto>> GetReadingsAsync(SensorReadingQueryDto query);
        Task<List<SensorReadingDto>> GetReadingsByDeviceIdAsync(int deviceId, DateTime? startDate, DateTime? endDate);
        Task<List<SensorReadingDto>> GetReadingsBySensorIdAsync(int sensorId, DateTime? startDate, DateTime? endDate);
    }
}
```

### 7.5 AlertService

```csharp
namespace IoTMonitoringSystem.Services
{
    public interface IAlertService
    {
        Task<List<AlertDto>> GetActiveAlertsAsync();
        Task<PagedResult<AlertDto>> GetAlertsAsync(AlertQueryDto query);
        Task<AlertDto> GetAlertByIdAsync(long alertId);
        Task<AlertDto> AcknowledgeAlertAsync(long alertId);
        Task<AlertDto> ResolveAlertAsync(long alertId);
        Task EvaluateAlertRulesAsync(SensorReadingDto reading);
    }
}
```

## 8. Data Access Layer

### 8.1 DbContext

```csharp
using Microsoft.EntityFrameworkCore;
using IoTMonitoringSystem.Core.Entities;

namespace IoTMonitoringSystem.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Device> Devices { get; set; }
        public DbSet<Sensor> Sensors { get; set; }
        public DbSet<SensorReading> SensorReadings { get; set; }
        public DbSet<DeviceStatusHistory> DeviceStatusHistories { get; set; }
        public DbSet<OperationalMetric> OperationalMetrics { get; set; }
        public DbSet<AlertRule> AlertRules { get; set; }
        public DbSet<Alert> Alerts { get; set; }
        public DbSet<DeviceConfiguration> DeviceConfigurations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and constraints
            modelBuilder.Entity<Sensor>()
                .HasOne(s => s.Device)
                .WithMany(d => d.Sensors)
                .HasForeignKey(s => s.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SensorReading>()
                .HasOne(sr => sr.Device)
                .WithMany(d => d.SensorReadings)
                .HasForeignKey(sr => sr.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SensorReading>()
                .HasOne(sr => sr.Sensor)
                .WithMany(s => s.SensorReadings)
                .HasForeignKey(sr => sr.SensorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            modelBuilder.Entity<SensorReading>()
                .HasIndex(sr => new { sr.Timestamp, sr.DeviceId });

            modelBuilder.Entity<Device>()
                .HasIndex(d => d.EdgeDeviceId)
                .IsUnique()
                .HasFilter("[EdgeDeviceId] IS NOT NULL");
        }
    }
}
```

### 8.2 Repository Pattern

```csharp
namespace IoTMonitoringSystem.Infrastructure.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<List<T>> GetAllAsync();
        Task<T> CreateAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        Task<bool> ExistsAsync(int id);
    }

    public interface IDeviceRepository : IRepository<Device>
    {
        Task<Device?> GetByEdgeDeviceIdAsync(string edgeDeviceId);
        Task<List<Device>> GetActiveDevicesAsync();
    }

    public interface ISensorReadingRepository : IRepository<SensorReading>
    {
        Task<List<SensorReading>> GetByDeviceIdAsync(int deviceId, DateTime? startDate, DateTime? endDate);
        Task<List<SensorReading>> GetBySensorIdAsync(int sensorId, DateTime? startDate, DateTime? endDate);
    }
}
```

## 9. Configuration and Startup

### 9.1 Program.cs Configuration

```csharp
using Microsoft.EntityFrameworkCore;
using IoTMonitoringSystem.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// SignalR
builder.Services.AddSignalR();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Dependency Injection
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<ISensorService, SensorService>();
builder.Services.AddScoped<ISensorReadingService, SensorReadingService>();
builder.Services.AddScoped<IAlertService, AlertService>();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.UseAuthorization();

app.MapControllers();
app.MapHub<MonitoringHub>("/hubs/monitoring");

app.Run();
```

### 9.2 appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=IoTMonitoringDB;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

## 10. Error Handling

### 10.1 Global Exception Handler

```csharp
namespace IoTMonitoringSystem.API.Middleware
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "An error occurred");

            var response = new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while processing your request",
                Errors = new List<string> { exception.Message }
            };

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

            return true;
        }
    }
}
```

## 11. Validation

### 11.1 FluentValidation

Use FluentValidation for DTO validation:

```csharp
using FluentValidation;

namespace IoTMonitoringSystem.Core.Validators
{
    public class CreateDeviceDtoValidator : AbstractValidator<CreateDeviceDto>
    {
        public CreateDeviceDtoValidator()
        {
            RuleFor(x => x.DeviceName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.DeviceType)
                .NotEmpty()
                .MaximumLength(50);
        }
    }
}
```

## 12. Notes

- This design follows ASP.NET Core best practices
- Entity classes match the database schema from Database Design Document
- Service layer handles business logic and validation
- Repository pattern provides data access abstraction
- SignalR enables real-time communication
- DTOs separate API contracts from entity models

---

## Approval

- **Prepared by:** [Your Name]
- **Reviewed by:** [Reviewer Name]
- **Approved by:** [Approver Name]
- **Date:** [Date]

---

## Notes

This document is a living document and will be updated as the application design evolves during development.

