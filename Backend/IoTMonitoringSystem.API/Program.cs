using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
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

// Database - use fallback so the app can start even if connection string is missing (health endpoint will still work).
var effectiveConnectionString = string.IsNullOrWhiteSpace(connectionString)
    ? "Server=(invalid);Database=missing;Connection Timeout=1;"
    : connectionString;

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(effectiveConnectionString));

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

// Auth service
builder.Services.AddScoped<IAuthService, AuthService>();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "IoTMonitoringSystem";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "IoTMonitoringFrontend";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // .NET 8 JwtBearer defaults MapInboundClaims=true, which maps JWT "role" to ClaimTypes.Role URI.
    // That breaks [Authorize(Roles=...)] when RoleClaimType is the short name "role" (matches our token).
    options.MapInboundClaims = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        // Must match the claim type used when writing the token
        RoleClaimType = "role",
        NameClaimType = "unique_name"
    };

    // Support JWT in SignalR (token passed as query param)
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/monitoringhub"))
                context.Token = accessToken;
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// SignalR with CORS support
builder.Services.AddSignalR();

// MQTT Service (Hosted Service)
builder.Services.AddSingleton<IMqttIngestMetrics, MqttIngestMetrics>();
builder.Services.AddSingleton<MqttService>();
builder.Services.AddSingleton<IMqttRuntimeState>(sp => sp.GetRequiredService<MqttService>());
builder.Services.AddSingleton<IMqttPublisher>(sp => sp.GetRequiredService<MqttService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<MqttService>());
builder.Services.AddHostedService<CommandTimeoutService>();

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
var mqttEnableTlsRaw = app.Configuration.GetValue<string>("Mqtt:EnableTls");
var mqttEnableTls = !string.IsNullOrWhiteSpace(mqttEnableTlsRaw) &&
    bool.TryParse(mqttEnableTlsRaw, out var parsedTls) && parsedTls;
var mqttUsername = app.Configuration.GetValue<string>("Mqtt:Username");
var corsOrigins = app.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()?
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .ToArray()
    ?? Array.Empty<string>();

if (string.IsNullOrWhiteSpace(connectionString))
{
    startupLogger.LogError(
        "ConnectionStrings__DefaultConnection is missing or empty. " +
        "Data API endpoints will fail. Set this value in Azure App Service Environment variables (use double underscores: ConnectionStrings__DefaultConnection).");
}
else
{
    startupLogger.LogInformation("Database connection string loaded (length={Length}).", connectionString.Length);
}

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

// Apply pending EF migrations automatically on startup, then seed default admin
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();

        // Seed default admin if no users exist yet
        if (!db.Users.Any())
        {
            var seedUsername = app.Configuration["AdminSeed:Username"] ?? "admin";
            var seedPassword = app.Configuration["AdminSeed:Password"] ?? "Admin@123";
            var seedEmail = app.Configuration["AdminSeed:Email"] ?? "admin@iot.local";
            db.Users.Add(new IoTMonitoringSystem.Core.Entities.User
            {
                Username = seedUsername,
                Email = seedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(seedPassword),
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
            var seedLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            seedLogger.LogInformation("Seeded default admin user '{Username}'.", seedUsername);
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while applying database migrations.");
    }
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

// Authentication must come BEFORE Authorization
app.UseAuthentication();
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
