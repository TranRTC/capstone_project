# MCP (Model Context Protocol) — IoT Monitoring API

The API exposes a **read-only** MCP server over HTTP so external AI clients (Cursor, Claude Desktop, custom agents) can query devices, alerts, sensors, actuators, readings, system health, and project documentation using the same tool layer as the in-dashboard assistant.

Write actions (acknowledge alert, toggle actuator, create device) stay in the dashboard with the confirm gate — they are **not** exposed via MCP.

## Endpoint

| Setting | Default | Description |
|---------|---------|-------------|
| `Mcp:Enabled` | `true` | Turn MCP HTTP transport on/off |
| `Mcp:HttpPath` | `/mcp` | URL path (e.g. `http://localhost:5000/mcp`) |
| `Mcp:RequireApiKey` | `true` (prod) | Require `X-Mcp-Api-Key` or `Authorization: Bearer` |
| `Mcp:ApiKey` | — | Shared secret when `RequireApiKey` is true |

Development (`appsettings.Development.json`) sets `RequireApiKey: false` for local testing.

## Tools (read-only)

- `get_devices` — list devices (optional `status` filter)
- `get_device` — device by ID
- `get_active_alerts` — unresolved alerts
- `get_alerts` — alerts with optional filters
- `get_sensors_by_device` — sensors for a device
- `get_actuators_by_device` — actuators for a device
- `get_recent_readings` — telemetry for a device (hours, default 24)
- `get_system_health` — MQTT/API health summary
- `search_documentation` — RAG over project docs

## Health check (JWT)

Authenticated dashboard users can call:

```
GET /api/v1/agent/mcp/status
```

## Cursor IDE

1. Start the API: `dotnet run` from `Backend/IoTMonitoringSystem.API`
2. **Project config (recommended):** create `.cursor/mcp.json` in the **repo root** (same folder as `README.md`). This folder is not created automatically — copy from `Documents/mcp/cursor-mcp.json` if needed.
3. **Or global config:** `C:\Users\<you>\.cursor\mcp.json` (works in every project).
4. **Or UI:** Cursor Settings → **MCP** → Add server → URL `http://localhost:5000/mcp`
5. Restart Cursor or **Developer: Reload Window**, then check Settings → MCP for a green `iot-monitoring` connection.

Example (development, no API key):

```json
{
  "mcpServers": {
    "iot-monitoring": {
      "url": "http://localhost:5000/mcp"
    }
  }
}
```

With API key (production-style):

```json
{
  "mcpServers": {
    "iot-monitoring": {
      "url": "http://localhost:5000/mcp",
      "headers": {
        "X-Mcp-Api-Key": "your-secret-key"
      }
    }
  }
}
```

## Architecture

```
MCP client (Cursor) ──HTTP──► MapMcp("/mcp")
                                    │
                                    ▼
                          IotMonitoringMcpTools
                                    │
                                    ▼
                          IIotAgentToolService  ◄── AgentToolExecutor (in-app chat)
                                    │
                                    ▼
                          Device / Alert / Sensor services → SQL Server
```

## Security notes

- MCP is bound to `localhost` / `127.0.0.1` in development via `RequireHost`.
- Use a strong `Mcp:ApiKey` in production and set `RequireApiKey: true`.
- Do not expose write tools over MCP without the same confirm/authorization model as the dashboard.
