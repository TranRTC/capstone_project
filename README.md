# Web-Based IoT Device Real-Time Monitoring System

A browser-based dashboard for monitoring IoT equipment in real time—collecting sensor data over MQTT, storing history in SQL Server, pushing live updates via SignalR, and sending device commands back to the edge.

**Capstone project** — BAS Application Development Program  
**Developed By:** Quoc Bao Tran
**Status:** As-built (May 2026)

---

## Why this project

Industrial, commercial, and agricultural sites need continuous equipment monitoring to stay safe and avoid failures. Traditional SCADA systems are expensive and tie you to proprietary hardware. This system offers a cost-effective alternative: a web dashboard accessible from any browser, built on open-source stack components, and compatible with standard IoT hardware (Arduino, ESP32, Raspberry Pi).

---

## Architecture

```mermaid
flowchart LR
  edge[EdgeDevices]
  mqtt[MQTTBroker]
  api[ASP.NETCoreAPI]
  db[(SQLServer)]
  hub[SignalRHub]
  spa[ReactDashboard]

  edge -->|readings_commands| mqtt
  mqtt --> api
  api --> db
  api --> hub
  hub --> spa
  spa -->|REST| api
```

| Layer | Responsibility |
|-------|----------------|
| **Edge** | Microcontrollers publish sensor readings and receive commands over MQTT |
| **Backend** | REST API, MQTT ingest, JWT auth, EF Core persistence |
| **Real-time** | SignalR hub at `/monitoringhub` streams readings, alerts, and device status |
| **Frontend** | React SPA with MUI dashboards, charts, and role-based navigation |
| **Database** | SQL Server — 11 tables for devices, sensors, readings, alerts, users, commands |

---

## Features

- **Real-time monitoring** — Live sensor readings and device online/offline status via SignalR
- **Dashboards and charts** — Recharts line trends, gauges, and discrete (0/1) indicators; live and historical windows
- **Alerting** — Threshold, range, and change rules; acknowledge and resolve; real-time notifications
- **Device management** — CRUD for devices, sensors, actuators, and configurations
- **Device control** — Commands over MQTT (`SetPower`, `SetValue`); command history page
- **Security** — JWT authentication with **Admin**, **Operator** (read/write), and **Viewer** (read-only) roles
- **MQTT pipeline** — Ingest readings, publish commands, health metrics endpoint
- **AI assistant (v1–v3)** — Chat, proactive insights, confirmed write actions, docs search, scheduled digests

---

## Technology stack

| Layer | Technologies |
|-------|--------------|
| Backend | C# ASP.NET Core 8, EF Core, SignalR, MQTTnet, JWT |
| Frontend | React 18, TypeScript, Material-UI, Recharts, Create React App |
| Database | SQL Server (LocalDB in development) |
| Edge / messaging | MQTT (Mosquitto local, HiveMQ cloud for demos) |
| CI/CD | GitHub Actions → Azure Web App + Azure Static Web Apps |

---

## Repository structure

```
capstone_project/
├── Backend/                        # Four .NET projects: API, Core, Infrastructure, Services
├── Frontend/iot-monitoring-frontend/ # React TypeScript SPA
├── Sensor Testing/                   # Python simulators, Arduino sketches, pipeline scripts
├── Documents/                        # Formal specs 001–010, testing, database scripts, slides
├── .github/workflows/                # Azure deployment pipelines
└── requirements.txt                  # Python deps for MQTT simulators (paho-mqtt)
```

| Folder | Purpose |
|--------|---------|
| [Backend/](Backend/) | Layered ASP.NET Core Web API |
| [Frontend/iot-monitoring-frontend/](Frontend/iot-monitoring-frontend/) | React dashboard |
| [Sensor Testing/](Sensor%20Testing/) | Hardware and simulator assets — see [Sensor Testing/README.md](Sensor%20Testing/README.md) |
| [Documents/](Documents/) | Full project documentation — start at [Documents/README.md](Documents/README.md) |

---

## Prerequisites

- Windows 10/11 (primary development environment)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) and npm
- SQL Server or **LocalDB** (default: `(localdb)\mssqllocaldb`)
- [Mosquitto](https://mosquitto.org/) or another MQTT broker on `localhost:1883`
- Optional: Python 3 + `pip install -r requirements.txt` for simulators
- Optional: Arduino IDE + Uno R4 WiFi for hardware testing

---

## Quick start (local)

### 1. Database

Default connection string in `Backend/IoTMonitoringSystem.API/appsettings.json`:

```
Server=(localdb)\mssqllocaldb;Database=IoTMonitoringDB;Trusted_Connection=True
```

The API applies EF Core migrations automatically on startup (`db.Database.Migrate()` in `Program.cs`). Start the backend once and the database is created.

To reset and re-seed: [Documents/database/README-Reset-Database.md](Documents/database/README-Reset-Database.md)

### 2. MQTT broker

Start Mosquitto on port **1883** (matches `appsettings.json` → `Mqtt:Host` / `Mqtt:Port`).

Optional firewall rules: `Sensor Testing/Add-MosquittoFirewallRules.ps1`

### 3. Backend

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
| REST base | http://localhost:5000/api/v1 |
| MQTT health | http://localhost:5000/api/v1/health/mqtt |

Default login (development seed): `admin` / `Admin@123`

### 4. Frontend

```powershell
cd Frontend/iot-monitoring-frontend
npm install --legacy-peer-deps
npm start
```

Web app: http://localhost:3000

Optional environment variables (defaults in `src/config/runtimeConfig.ts`):

```
REACT_APP_API_BASE_URL=http://localhost:5000/api/v1
REACT_APP_SIGNALR_HUB_URL=http://localhost:5000/monitoringhub
```

### 5. Send test data

**Python simulator (no hardware):**

```powershell
pip install -r requirements.txt
python "Sensor Testing/simulator-local.py"
```

**Automated API tests:**

```powershell
powershell -ExecutionPolicy Bypass -File "Documents/testing/Run-ApiTests.ps1"
```

Register a device and sensors in the UI first so simulator device/sensor IDs match. See [Sensor Testing/README.md](Sensor%20Testing/README.md) for Arduino sketches and ID setup.

---

## MQTT topics

| Direction | Topic |
|-----------|--------|
| Device → API | `devices/{deviceId}/sensors/{sensorId}/readings` |
| API → Device | `devices/{deviceId}/commands` |
| Device → API | `devices/{deviceId}/commands/ack` |

Reading payload: `{"value": 22.5}` (analog) or `{"value": 1}` (discrete).

---

## Live demo (Azure)

Deployed via GitHub Actions on push to `main`:

| Component | URL |
|-----------|-----|
| **Dashboard** | https://tran.iot-dashboard.app/ |
| API | https://capstoneiotdashboard-gudbg0amfxeehae5.westus-01.azurewebsites.net |
| Swagger | https://capstoneiotdashboard-gudbg0amfxeehae5.westus-01.azurewebsites.net/swagger |

Cloud pipeline check: `Sensor Testing/Check-CloudPipeline.ps1`

---

## AI Assistant (v1 chat + v2 proactive)

Open **Assistant** in the sidebar (`/assistant`) to ask questions about live monitoring data or review **proactive insights** generated automatically when the system detects issues.

### v1 — On-demand chat

Example questions:
- "List all devices"
- "Any active alerts?"
- "Recent readings for device 1"
- "Is MQTT connected?"

### v2 — Proactive monitoring

The backend watches for:
- **New active alerts** (after alert rules fire)
- **Offline devices** (no readings within configured minutes)
- **MQTT pipeline issues** (broker disconnected)

Insights appear in the Assistant feed and via SignalR (`AgentInsightCreated`). Each insight includes a summary, suggested next steps, dismiss, and **Ask follow-up** (opens v1 chat with context).

Configure in `Agent:Proactive` (see `appsettings.json`):
- `Enabled`, `DeviceOfflineMinutes`, `MqttUnhealthyMinutes`, `SweepIntervalSeconds`
- `InsightCooldownMinutes`, `MaxInsightsPerHour`

v2 remains **read-only** for automatic insights. **v3** adds confirmed write actions, documentation search (RAG), and scheduled digests — see below.

### v3 — Confirmed actions, docs (RAG), scheduled digests

**Write actions (Admin/Operator):** The assistant can *propose* actions; you must **Confirm** in chat before anything runs:
- Acknowledge / resolve alerts
- Create devices
- Send actuator commands (`SetPower` on/off, `SetValue` for analog)

**Documentation (RAG):** Ask setup or troubleshooting questions (e.g. *“How do I deploy to Azure?”*, *“What MQTT topics does the edge use?”*). The assistant searches indexed project markdown and cites sources.

**Scheduled digests:** Daily (and weekly) summary insights appear in the proactive feed with trigger types `DailyDigest` / `WeeklyDigest`.

Configure in `Agent:Actions`, `Agent:Rag`, and `Agent:ScheduledReports` (see `appsettings.json`).

Admins can manually trigger a digest for demos:
```http
POST /api/v1/agent/reports/run-digest?type=Daily
Authorization: Bearer <admin-jwt>
```

### MCP (Model Context Protocol)

The API hosts a **read-only** MCP server at `http://localhost:5000/mcp` (configurable via `Mcp:HttpPath`). External clients such as **Cursor** can call the same data tools as the in-dashboard assistant without duplicating business logic.

- **Tools:** devices, alerts, sensors, actuators, readings, system health, documentation search
- **Writes:** not exposed over MCP (use dashboard confirm flow)
- **Setup:** see [Documents/MCP.md](Documents/MCP.md) and `Documents/mcp/cursor-mcp.json`
- **Status:** `GET /api/v1/agent/mcp/status` (JWT)

### Professional assistant (v4)

The in-dashboard assistant adds operator-grade features:

| Feature | Description |
|---------|-------------|
| **Context-aware chat** | Pass `deviceId` via `/assistant?deviceId=1` or from device page link |
| **Intent router** | Fast answers for alerts, health, offline devices without extra LLM tool loops |
| **Analytics tools** | `get_alert_summary`, `get_operational_snapshot`, `get_sensor_reading_summary`, `find_devices` |
| **Audit log** | All chats, tool calls, and confirmed actions (Admin: `GET /api/v1/agent/audit`) |
| **Metrics** | 24h usage stats (Admin: `GET /api/v1/agent/metrics`) |
| **Server-side sessions** | Chat history persisted per user (`sessionId` returned from chat) |
| **Data citations** | Replies include live data timestamp and tools used |
| **Suggested prompts** | `GET /api/v1/agent/suggested-prompts` |

Config: `Agent:IntentRouter`, `Agent:Sessions` in `appsettings.json`.

### Local development — Ollama (free)

Development defaults in `appsettings.Development.json` use **Ollama** on `http://localhost:11434/v1` with model `llama3.2`.

1. Install [Ollama](https://ollama.com)
2. Pull the model:
   ```powershell
   ollama pull llama3.2
   ```
3. Ensure Ollama is running (it usually starts with Windows; or run `ollama serve`)
4. Restart the backend API (`ASPNETCORE_ENVIRONMENT=Development`)

No cloud API key required for local dev.

### Production / cloud LLM (optional)

For Azure or OpenAI instead of Ollama, set in App Service or user-secrets:

```powershell
cd Backend/IoTMonitoringSystem.API
dotnet user-secrets set "Agent:Llm:ApiKey" "YOUR_OPENAI_API_KEY"
dotnet user-secrets set "Agent:Llm:BaseUrl" "https://api.openai.com/v1"
dotnet user-secrets set "Agent:Model" "gpt-4o-mini"
```

Or use **Groq** (free tier): `Agent:Llm:BaseUrl` = `https://api.groq.com/openai/v1`

Restart the API after changing LLM settings.

---

## Testing

| Resource | Purpose |
|----------|---------|
| [Documents/testing/Run-ApiTests.ps1](Documents/testing/Run-ApiTests.ps1) | Automated REST API tests |
| [Documents/testing/MANUAL_TEST_CHECKLIST.md](Documents/testing/MANUAL_TEST_CHECKLIST.md) | UI, SignalR, MQTT, demo checklist |
| [Documents/007_TestingPlan.md](Documents/007_TestingPlan.md) | Formal test strategy |

---

## Documentation

**Start here:** [Documents/README.md](Documents/README.md)

| # | Document |
|---|----------|
| 1 | [001_Overview](Documents/001_Overview.md) |
| 2 | [002_Requirements](Documents/002_Requirements.md) |
| 3 | [003_DatabaseDesign](Documents/003_DatabaseDesign.md) |
| 4 | [004_ApplicationDesign](Documents/004_ApplicationDesign.md) |
| 5 | [005_FrontendDesign](Documents/005_FrontendDesign.md) |
| 6 | [006_APIDocumentation](Documents/006_APIDocumentation.md) |
| 7 | [007_TestingPlan](Documents/007_TestingPlan.md) |
| 8 | [008_DeploymentGuide](Documents/008_DeploymentGuide.md) |
| 9 | [009_ImplementationGuide](Documents/009_ImplementationGuide.md) |
| 10 | [010_UserManual](Documents/010_UserManual.md) |

Also: [Documents/testing/](Documents/testing/) · [Documents/Presentation/](Documents/Presentation/) · [Documents/database/](Documents/database/)

---

## Deployment

- **Local / full setup:** [Documents/008_DeploymentGuide.md](Documents/008_DeploymentGuide.md)
- **Backend CI:** [.github/workflows/main_capstoneiotdashboard.yml](.github/workflows/main_capstoneiotdashboard.yml) → Azure Web App
- **Frontend CI:** [.github/workflows/azure-static-web-apps-yellow-forest-08dd65c0f.yml](.github/workflows/azure-static-web-apps-yellow-forest-08dd65c0f.yml) → Azure Static Web Apps

Do not commit production secrets (JWT key, MQTT credentials, connection strings). Use Azure App Settings or user secrets for production.

---

## License

Academic capstone project. All rights reserved unless otherwise noted.
