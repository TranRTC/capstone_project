using Microsoft.EntityFrameworkCore;
using IoTMonitoringSystem.Infrastructure.Data;
using IoTMonitoringSystem.Infrastructure.Repositories;
using IoTMonitoringSystem.Services;
using IoTMonitoringSystem.Core.Interfaces;
using IoTMonitoringSystem.API.Services;
using IoTMonitoringSystem.API.Hubs;

var builder = WebApplication.CreateBuilder(args);

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
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<ISensorReadingRepository, SensorReadingRepository>();
builder.Services.AddScoped<IAlertRepository, AlertRepository>();

// Notification Service (SignalR)
builder.Services.AddScoped<INotificationService, SignalRNotificationService>();

// Services
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<ISensorService, SensorService>();
builder.Services.AddScoped<ISensorReadingService, SensorReadingService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IAlertRuleService, AlertRuleService>();

// SignalR with CORS support
builder.Services.AddSignalR();

// MQTT Service (Hosted Service)
builder.Services.AddHostedService<MqttService>();

// CORS - Must be configured before SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000", 
                "https://localhost:3000",
                "http://localhost:5173",
                "https://localhost:5173"
              )
              .AllowAnyHeader()  // This allows all headers including x-requested-with
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

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
