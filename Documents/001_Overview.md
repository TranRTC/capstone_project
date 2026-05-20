# Project Overview

## Project Name
Web-Based IoT Device Real-Time Monitoring System

## Purpose

### Application Purpose (Why This System is Needed)
There is a need for continuous monitoring of equipment/device in industrial, commercial, and agricultural facilities to ensure operational safety, prevent failures, and optimize performance. This web-based solution monitors IoT device status in real-time and stores historical data for trace-back analysis and data-driven decision-making. It enables remote monitoring of distributed IoT devices across multiple locations, serving as a cost-effective alternative to industrial SCADA systems, which are expensive and dependent on specific hardware and software requirements.

### Capstone Project Purpose (Why This Project Exists)
This capstone project applies knowledge from the Application Development Program (programming languages, databases, software engineering, project management) and hands-on hardware experience (sensors, microcontrollers like Arduino and ESP modules, single-board computers like Raspberry Pi) to solve a real-world problem through technology integration (C# backend, React frontend, RESTful API, SignalR, SQL Server). It demonstrates the application of academic knowledge to practical solutions, serving as evidence of technical expertise for portfolio and professional use.

## Project Description
The Industrial Equipment Real-Time Monitoring System is a comprehensive solution that:
- **Monitors** IoT equipment and devices in real-time
- **Collects** sensor data, operational metrics, and status information
- **Visualizes** data through interactive dashboards and charts
- **Alerts** users to critical events, anomalies, or threshold breaches
- **Tracks** historical performance and trends
- **Supports** multiple devices and equipment types simultaneously

## Technical Stack (as implemented)
- **Backend:** C# ASP.NET Core 8 Web API (layered: API, Core, Infrastructure, Services)
- **Frontend:** React 18 + TypeScript, Material-UI (MUI), Create React App
- **Real-Time:** SignalR hub at `/monitoringhub`
- **Edge ingest:** MQTT (Mosquitto); topics `devices/{id}/sensors/{id}/readings`
- **Database:** SQL Server (LocalDB in development), 11 tables via EF Core migrations
- **Charts:** Recharts (device detail and dashboard)
- **Security:** JWT authentication; roles Admin, Operator, Viewer

## Architecture Overview
- **Edge Devices:** Microcontrollers (Arduino, ESP modules) and single-board computers (Raspberry Pi) that interface with sensors and send data to the backend via HTTP/MQTT
- **Backend API:** RESTful API built with ASP.NET Core for request-response operations (device management, historical data queries, CRUD operations)
- **Data Layer:** SQL Server database for persistent storage of historical data, sensor readings, operational metrics, and configurations
- **Real-Time Hub:** SignalR hub for server-to-client push communication, streaming live sensor data and instant notifications to connected clients
- **Frontend Application:** React SPA with component-based architecture, displaying real-time data via SignalR and historical analytics via RESTful API

## Key Features (implemented)
1. **Real-Time Data Streaming**
   - Live sensor data via SignalR (`SensorReadingReceived`, alert events)
   - Dashboard and device detail charts update without refresh
   - Equipment status monitoring (`DeviceStatusChanged`)
   - Performance metrics tracking

2. **Dashboard and visualization**
   - Dashboard with device counts, active alerts, API/MQTT/SignalR status
   - Recharts on device detail (live + historical readings)

3. **Alerting**
   - Alert rules (threshold, range, change) per device/sensor
   - Active alerts with acknowledge and resolve; SignalR notifications

4. **Device management**
   - CRUD for devices, sensors, actuators, configurations
   - Device commands via API → MQTT (`devices/{id}/commands`)
   - Command history page with status filter

5. **User interface and security**
   - MUI layout with sidebar navigation (Dashboard, Devices, Sensors, Actuators, Alert Rules, Alerts, Command History, Users for Admin)
   - JWT login; **Admin**, **Operator** (read/write), **Viewer** (read-only)

## Development URLs (local)

| Service | URL |
|---------|-----|
| API | http://localhost:5000 |
| Swagger | http://localhost:5000/swagger |
| SignalR | http://localhost:5000/monitoringhub |
| Frontend | http://localhost:3000 |
| Default admin | `admin` / `Admin@123` (development seed) |

## Current status
- Backend API, database (11 tables), MQTT ingest, and SignalR implemented
- React frontend with all main routes implemented
- Automated API tests: `Documents/testing/Run-ApiTests.ps1`

## Key Tools
- Git + GitHub for version control
- Google Drive for disaster backups (ZIP files only)
- Windows development environment

## Important Rules
- Do NOT work inside Google Drive folders
- Commit and push to GitHub regularly
- Take ZIP snapshot backups weekly or before risky changes

## Development phases (completed for capstone)

| Phase | Deliverable | Status |
|-------|-------------|--------|
| 1 | Solution structure, EF Core, SQL Server | Done |
| 2 | REST API, JWT auth, Swagger | Done |
| 3 | SignalR `MonitoringHub`, live notifications | Done |
| 4 | MQTT subscriber + command publisher | Done |
| 5 | React SPA, MUI, routing | Done |
| 6 | Charts, alerts, actuators, commands | Done |
| 7 | Testing plan + manual/automated tests | Done |
| 8 | Documentation (`Documents/`) and deployment guide | Done |

## Documentation layout

All written project materials are under **`Documents/`**:

| Path | Contents |
|------|----------|
| `Documents/001_Overview.md` … `010_UserManual.md` | Formal capstone documents (this series) |
| `Documents/testing/` | Manual test checklist, automated API results, `Run-ApiTests.ps1` |
| `Documents/Presentation/` | Capstone PowerPoint and slide tables |

Index: [README.md](README.md)

## Notes
This file is the main project overview.  
**Status:** As-Built (aligned with codebase, May 2026).  
When starting a new chat or returning after a break, read this first.


