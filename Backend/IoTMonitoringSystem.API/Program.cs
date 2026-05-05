using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using IoTMonitoringSystem.Infrastructure.Data;
using IoTMonitoringSystem.Infrastructure.Repositories;
using IoTMonitoringSystem.Services;
using IoTMonitoringSystem.Core.Interfaces;
using IoTMonitoringSystem.API.Services;
using IoTMonitoringSystem.API.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Azure env templates sometimes use Mqtt__BrokerHost / Mqtt__BrokerPort; runtime code reads Mqtt:Host / Mqtt:Port.
ApplyMqttBrokerAliases(builder.Configuration);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string is missing. Set ConnectionStrings__DefaultConnection in Azure App Service (Environment variables / Configuration).");
}

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure JSON serialization to use camelCase (matching frontend)
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Repositories
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<ISensorReadingRepository, SensorReadingRepository>();
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<IDeviceCommandRepository, DeviceCommandRepository>();
builder.Services.AddScoped<IDeviceConfigurationRepository, DeviceConfigurationRepository>();
builder.Services.AddScoped<IActuatorRepository, ActuatorRepository>();

// Notification Service (SignalR)
builder.Services.AddScoped<INotificationService, SignalRNotificationService>();
builder.Services.AddScoped<IDeviceCommandDispatcher, MqttCommandDispatcher>();

// Services
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<ISensorService, SensorService>();
builder.Services.AddScoped<ISensorReadingService, SensorReadingService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IAlertRuleService, AlertRuleService>();
builder.Services.AddScoped<IDeviceCommandService, DeviceCommandService>();
builder.Services.AddScoped<IDeviceConfigurationService, DeviceConfigurationService>();
builder.Services.AddScoped<IActuatorService, ActuatorService>();

// SignalR with CORS support
builder.Services.AddSignalR();

// MQTT Service (Hosted Service)
builder.Services.AddSingleton<IMqttIngestMetrics, MqttIngestMetrics>();
builder.Services.AddSingleton<MqttService>();
builder.Services.AddSingleton<IMqttRuntimeState>(sp => sp.GetRequiredService<MqttService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<MqttService>());

// CORS - Must be configured before SignalR
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()?
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .ToArray()
        ?? Array.Empty<string>();

    options.AddPolicy("AllowReactApp", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
            return;
        }

        // Fallback for local development if no config is provided.
        policy.WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000",
                "http://localhost:5173",
                "https://localhost:5173"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();
var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("StartupConfig");
var mqttHost = app.Configuration.GetValue<string>("Mqtt:Host", "localhost");
var mqttPort = app.Configuration.GetValue<int>("Mqtt:Port", 1883);
var mqttEnableTls = app.Configuration.GetValue<bool>("Mqtt:EnableTls", false);
var mqttUsername = app.Configuration.GetValue<string>("Mqtt:Username");
var corsOrigins = app.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()?
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .ToArray()
    ?? Array.Empty<string>();

startupLogger.LogInformation(
    "Startup configuration: MQTT {Host}:{Port} (TLS={TlsEnabled}, UsernameConfigured={HasUsername}), CORS origins count {OriginCount}.",
    mqttHost,
    mqttPort,
    mqttEnableTls,
    !string.IsNullOrWhiteSpace(mqttUsername),
    corsOrigins.Length);
if (!app.Environment.IsDevelopment() && !mqttEnableTls)
{
    startupLogger.LogWarning("Production/staging environment detected with MQTT TLS disabled. Enable Mqtt:EnableTls for secure transport.");
}
if (corsOrigins.Length > 0)
{
    startupLogger.LogInformation("Configured CORS origins: {Origins}", string.Join(", ", corsOrigins));
}
else
{
    startupLogger.LogWarning("No Cors:AllowedOrigins configured; localhost fallback origins will be used.");
}

// Configure the HTTP request pipeline
// CORS must come FIRST, before any other middleware that might redirect
app.UseCors("AllowReactApp");

// Completely disable HTTPS redirection - it causes 307 redirects that break CORS
// DO NOT enable HTTPS redirection in development
// The backend should run on HTTP (port 5000) only in development

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

// SignalR Hub - CORS is handled by UseCors middleware above
// SignalR will use the CORS policy configured above
app.MapHub<MonitoringHub>("/monitoringhub");

app.Run();

static void ApplyMqttBrokerAliases(ConfigurationManager configuration)
{
    var patches = new Dictionary<string, string?>();
    if (string.IsNullOrWhiteSpace(configuration["Mqtt:Host"]) &&
        !string.IsNullOrWhiteSpace(configuration["Mqtt:BrokerHost"]))
    {
        patches["Mqtt:Host"] = configuration["Mqtt:BrokerHost"];
    }

    if (string.IsNullOrWhiteSpace(configuration["Mqtt:Port"]) &&
        !string.IsNullOrWhiteSpace(configuration["Mqtt:BrokerPort"]))
    {
        patches["Mqtt:Port"] = configuration["Mqtt:BrokerPort"];
    }

    if (patches.Count > 0)
    {
        configuration.AddInMemoryCollection(patches);
    }
}
