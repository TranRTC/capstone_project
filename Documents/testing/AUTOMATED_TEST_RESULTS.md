# Automated Test Results

**Date:** 2026-05-19 (re-run after doc alignment)  
**Runner:** `Documents/testing/Run-ApiTests.ps1`  
**API base:** `http://localhost:5000`  
**Login:** `admin` / `Admin@123`

## Summary

| Result | Count |
|--------|-------|
| **Passed** | **18 / 18** |
| Failed | 0 |

## Phase 1 — Health

| Id | Test | Result |
|----|------|--------|
| 1.1 | GET /api/v1/health | Pass (200) |
| 1.2 | GET /api/v1/health/mqtt | Pass (200) |
| 1.3 | Swagger UI | Pass (200) |

## Phase 2 — Auth

| Id | Test | Result |
|----|------|--------|
| 2.2 | GET /devices without token | Pass (401) |
| 2.1 | POST /auth/login | Pass (token received) |
| 2.4 | Bad password | Pass (401) |

## Phase 3 — REST (authorized)

| Id | Test | Result |
|----|------|--------|
| 3.1.1 | POST /devices | Pass (device created) |
| 3.1.2 | GET /devices | Pass (200) |
| 3.1.3 | GET /devices/{id} | Pass (200) |
| 3.1.5 | GET /devices/{id}/status | Pass (200) |
| 3.2.1 | POST sensor | Pass (sensor created) |
| 3.2.2 | GET sensors by device | Pass (200) |
| 3.3.1 | POST sensorreading | Pass (201) |
| 3.3.2 | GET sensorreadings/devices/{id}/readings | Pass (200) |
| 3.4.1 | POST alertrule | Pass (201) |
| 3.4.2 | GET alerts/active | Pass (200) |
| 3.4.x | GET alertrules | Pass (200) |
| 3.7.1 | GET /auth/users | Pass (200) |

## Script fixes applied this run

- Parse `deviceId` / `sensorId` from API `data` wrapper (camelCase JSON).
- Alert rule payload uses `ruleType`, `condition`, `comparisonOperator` (matches API DTO).
- Readings URL: `GET /api/v1/sensorreadings/devices/{deviceId}/readings`.

## Not covered by automation

- Frontend UI (all screens)
- SignalR live updates in browser
- MQTT / physical devices
- SSMS / database screenshots
- End-to-end presentation demo

→ [MANUAL_TEST_CHECKLIST.md](MANUAL_TEST_CHECKLIST.md)

## Re-run

```powershell
powershell -ExecutionPolicy Bypass -File "Documents/testing/Run-ApiTests.ps1"
```
