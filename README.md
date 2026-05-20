# IoT Device Real-Time Monitoring System

A web-based IoT monitoring solution with real-time data streaming, alerting, and visualization.

## Project structure

```
capstone_project/
├── Backend/              # ASP.NET Core Web API
├── Frontend/             # React TypeScript SPA
├── Documents/            # All project documentation (see below)
│   ├── 001_Overview.md … 010_UserManual.md
│   ├── testing/          # Manual checklist, automated results, API test script
│   └── Presentation/     # Capstone slides and tables
└── temperature-sensor-simulator.py   # MQTT test simulator (optional)
```

## Quick start

### Backend

```powershell
cd Backend
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project IoTMonitoringSystem.API/IoTMonitoringSystem.API.csproj
```

| Service | URL |
|---------|-----|
| API | http://localhost:5000 |
| Swagger | http://localhost:5000/swagger |
| SignalR | http://localhost:5000/monitoringhub |

Default login (development): `admin` / `Admin@123`

### Frontend

```powershell
cd Frontend/iot-monitoring-frontend
npm install --legacy-peer-deps
npm start
```

Web app: http://localhost:3000

## Documentation

**Start here:** [Documents/README.md](Documents/README.md)

| Location | Contents |
|----------|----------|
| [Documents/](Documents/) | Formal specs `001`–`010` |
| [Documents/testing/](Documents/testing/) | Test plan link, manual checklist, automated results, `Run-ApiTests.ps1` |
| [Documents/Presentation/](Documents/Presentation/) | PowerPoint and Excel for capstone slides |

## Technology stack

- **Backend:** ASP.NET Core 8, EF Core, SignalR, MQTT
- **Frontend:** React, TypeScript, Material-UI
- **Database:** SQL Server (LocalDB or full instance)

## Features

- Device, sensor, and actuator management
- Real-time readings via SignalR
- Alert rules and notifications
- MQTT ingest and device commands
- Role-based users (Admin, Operator, Viewer)
