# IoT Device Real-Time Monitoring System

A comprehensive web-based IoT monitoring solution with real-time data streaming, alerting, and data visualization.

## 📁 Project Structure

```
capstone_project/
├── Backend/              # ASP.NET Core Web API
│   ├── IoTMonitoringSystem.API/
│   ├── IoTMonitoringSystem.Core/
│   ├── IoTMonitoringSystem.Infrastructure/
│   ├── IoTMonitoringSystem.Services/
│   └── IoTMonitoringSystem.slnx
│
├── Frontend/             # React TypeScript Application
│   └── iot-monitoring-frontend/
│
└── Documents/            # Project Documentation
    ├── context/          # Design documents (001-010)
    ├── *.md              # Guides and instructions
    └── *.ps1             # Scripts
```

## 🚀 Quick Start

### Backend Setup

```powershell
cd Backend
dotnet restore
dotnet ef database update --project IoTMonitoringSystem.Infrastructure/IoTMonitoringSystem.Infrastructure.csproj
dotnet run --project IoTMonitoringSystem.API/IoTMonitoringSystem.API.csproj
```

Backend runs on:
- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001

### Frontend Setup

```powershell
cd Frontend/iot-monitoring-frontend
npm install --legacy-peer-deps
npm start
```

Frontend runs on: http://localhost:3000

## 📚 Documentation

All documentation is in the `Documents/` folder:
- Design documents (001-010)
- API testing guides
- Installation instructions
- Troubleshooting guides

## 🛠️ Technology Stack

- **Backend:** ASP.NET Core Web API, Entity Framework Core, SignalR
- **Frontend:** React, TypeScript, Material-UI, SignalR Client
- **Database:** SQL Server (LocalDB)
- **Real-Time:** SignalR WebSockets

## 📋 Features

- ✅ Device Management (CRUD)
- ✅ Sensor Management
- ✅ Real-Time Sensor Data Streaming
- ✅ Alert System with Rule Evaluation
- ✅ Data Visualization
- ✅ RESTful API
- ✅ SignalR Real-Time Updates

## 📖 Getting Started

See `Documents/` folder for detailed guides:
- Installation instructions
- API testing guides
- Deployment guides
- User manuals
