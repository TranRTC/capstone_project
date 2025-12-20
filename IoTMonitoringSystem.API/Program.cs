using Microsoft.EntityFrameworkCore;
using IoTMonitoringSystem.Infrastructure.Data;
using IoTMonitoringSystem.Infrastructure.Repositories;
using IoTMonitoringSystem.Services;
using IoTMonitoringSystem.Core.Interfaces;
using IoTMonitoringSystem.API.Services;
using IoTMonitoringSystem.API.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
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

// SignalR
builder.Services.AddSignalR();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.UseAuthorization();

app.MapControllers();
app.MapHub<MonitoringHub>("/monitoringhub");

app.Run();
