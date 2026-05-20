# Manual Test Checklist — IoT Monitoring System

Automated API tests: [Run-ApiTests.ps1](Run-ApiTests.ps1)  
Formal test plan: [007_TestingPlan.md](../007_TestingPlan.md)

**Mark each item:** ☐ Not run · ✅ Pass · ❌ Fail · Notes: ___________

---

## Prerequisites (manual setup)

| # | Task | Done |
|---|------|------|
| M0.1 | SQL Server running; API applies migrations | ☐ |
| M0.2 | API running (`http://localhost:5000` or port from console) | ☐ |
| M0.3 | Frontend running (`http://localhost:3000`) | ☐ |
| M0.4 | MQTT broker running (if testing ingest/commands) | ☐ |
| M0.5 | Login: `admin` / `Admin@123` (unless changed in config) | ☐ |

---

## Phase 4 — Database verification (manual)

| # | Test | Steps | Pass |
|---|------|--------|------|
| M4.1 | Tables exist | SSMS / Azure Data Studio → 11 tables | ☐ |
| M4.2 | Data matches API | After API creates device, query `Devices` / `SensorReadings` | ☐ |
| M4.3 | Screenshot for presentation | ERD or table list + sample rows | ☐ |

---

## Phase 5 — Frontend UI (manual)

Login at `http://localhost:3000/login`

| # | Screen | What to verify | Pass |
|---|--------|----------------|------|
| M5.1 | Login | Valid login → dashboard; bad password → error | ☐ |
| M5.2 | Dashboard | Device count, active alerts, API / MQTT / SignalR status | ☐ |
| M5.3 | Devices | Add, edit, delete device; list updates | ☐ |
| M5.4 | Device detail | Chart loads; sensors; breadcrumbs | ☐ |
| M5.5 | Device detail — sensors | Add/edit sensor via SensorForm | ☐ |
| M5.6 | Device detail — actuators | Add actuator; control UI | ☐ |
| M5.7 | Device detail — alert rules | Create rule on device page | ☐ |
| M5.8 | Device detail — commands | Send command; see feedback / status | ☐ |
| M5.9 | Sensors page | Cross-device sensor list and CRUD | ☐ |
| M5.10 | Actuators page | Actuator CRUD | ☐ |
| M5.11 | Alert rules page | List and manage rules | ☐ |
| M5.12 | Alerts page | List; acknowledge; resolve | ☐ |
| M5.13 | Command history | Filter by status; pagination | ☐ |
| M5.14 | Users (Admin) | `/users` visible; create/delete if needed | ☐ |
| M5.15 | Users (non-admin) | Viewer — Users menu hidden / route blocked | ☐ |
| M5.16 | Logout | Cannot open dashboard without login | ☐ |
| M5.17 | UI consistency | Navigation, theme, readable layout | ☐ |

---

## Phase 6 — Real-time / SignalR (manual)

| # | Test | Steps | Pass |
|---|------|--------|------|
| M6.1 | SignalR connected | Dashboard shows connected | ☐ |
| M6.2 | Live reading | Post reading (Swagger or MQTT) → chart updates without F5 | ☐ |
| M6.3 | Live alert | Reading over threshold → alert without refresh | ☐ |
| M6.4 | WebSocket | F12 → Network → WS to `/monitoringhub` | ☐ |
| M6.5 | Reconnect | Restart API briefly → UI reconnects | ☐ |

---

## Phase 7 — MQTT and edge (manual)

| # | Test | Steps | Pass |
|---|------|--------|------|
| M7.1 | Health MQTT | `GET /api/v1/health/mqtt` or dashboard MQTT status | ☐ |
| M7.2 | Publish reading | Simulator or device → `devices/{id}/sensors/{id}/readings` | ☐ |
| M7.3 | DB + UI | Reading in SQL + live dashboard update | ☐ |
| M7.4 | Command to device | UI command → MQTT → command status updates | ☐ |
| M7.5 | Physical device (optional) | ESP32/Arduino sends real data | ☐ |

---

## Phase 8 — End-to-end demo (presentation)

Target **5–10 minutes**.

| Step | Action | Pass |
|------|--------|------|
| D1 | Login as admin | ☐ |
| D2 | Dashboard — status + active alerts | ☐ |
| D3 | Devices — show test device | ☐ |
| D4 | Device detail — chart | ☐ |
| D5 | Trigger alert (hot reading or MQTT) | ☐ |
| D6 | Alerts — acknowledge or resolve | ☐ |
| D7 | Command history (if applicable) | ☐ |
| D8 | Optional: Swagger GET with Authorize | ☐ |

---

## Phase 9 — Error and edge cases (manual)

| # | Test | Expected | Pass |
|---|------|----------|------|
| M9.1 | API stopped, UI open | Friendly error, no white screen | ☐ |
| M9.2 | Invalid device URL `/devices/99999` | 404 or not found message | ☐ |
| M9.3 | SQL Server stopped | Clear error on login/data | ☐ |
| M9.4 | Expired session | Redirect to login after idle | ☐ |
| M9.5 | Operator vs Admin | Operator cannot access User management | ☐ |

---

## Swagger UI (manual)

| # | Task | Pass |
|---|------|------|
| MS1 | Open `http://localhost:5000/swagger` | ☐ |
| MS2 | `POST /auth/login` → copy token | ☐ |
| MS3 | Authorize → `Bearer <token>` | ☐ |
| MS4 | `GET /devices` and one POST | ☐ |

---

## Test log

| Date | Tester | Automated (script) | Manual phases done | Notes |
|------|--------|--------------------|--------------------|-------|
| | | | | |

---

## Reference

- Automated results: [AUTOMATED_TEST_RESULTS.md](AUTOMATED_TEST_RESULTS.md)
- API details: [006_APIDocumentation.md](../006_APIDocumentation.md)
- Running the app: [009_ImplementationGuide.md](../009_ImplementationGuide.md)
