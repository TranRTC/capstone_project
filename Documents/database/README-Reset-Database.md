# Reset database (empty tables, IDs start at 1)

## Local database (your PC)

**Connection** (from `Backend/IoTMonitoringSystem.API/appsettings.json`):

- Server: `(localdb)\mssqllocaldb`
- Database: `IoTMonitoringDB`

### Steps

1. **Stop** the backend API if it is running.
2. Open **SQL Server Management Studio** or **Azure Data Studio**.
3. Connect to `(localdb)\mssqllocaldb`.
4. Open and run: `Documents/database/Reset-IoTDatabase.sql`
5. Confirm output shows **0 rows** for Devices/Sensors/Users.
6. Start the API: `dotnet run` from `Backend/IoTMonitoringSystem.API`.
7. Log in to **localhost:3000** with **admin** / **Admin@123** (re-seeded automatically).

### Create data in order (so IDs are 1, 2, 3…)

| Step | UI action | Resulting ID |
|------|-----------|--------------|
| 1 | Add **one device** | DeviceId = **1** |
| 2 | Add temp sensor on device 1 | SensorId = **1** |
| 3 | Add humidity sensor | SensorId = **2** |
| 4 | Add digital input sensors (discrete) | **3**, **4** |
| 5 | Add actuators (DO channel 7, 8; AO channel 3) | **1**, **2**, **3** |

Then set your Arduino sketch:

```cpp
const int DEVICE_ID       = 1;
const int SENSOR_TEMP_ID  = 1;
const int SENSOR_HUM_ID   = 2;
const int SENSOR_DI1_ID   = 3;
const int SENSOR_DI2_ID   = 4;
```

Run the same script on **cloud** Azure SQL only if you use a **separate** cloud database (change `USE [IoTMonitoringDB]` if your Azure DB name differs).

## Cloud database (Azure SQL)

1. Azure Portal → your SQL database → **Query editor** (or SSMS with firewall rule).
2. Paste the same `DELETE` / `DBCC CHECKIDENT` section from `Reset-IoTDatabase.sql` (adjust `USE [YourCloudDbName];`).
3. Restart the **Azure Web App** (CapstoneIoTDashboard).
4. Create device/sensors in the **cloud** dashboard (not localhost).

Local and cloud are **two different databases** — reset each one you use.

## Why IDs were large (e.g. 1026)

SQL Server **IDENTITY** never reuses numbers after rows are deleted unless you run `DBCC CHECKIDENT ... RESEED, 0`. Creating and deleting test devices leaves the counter high. The reset script fixes that.

## Optional: nuclear option (local only)

Drop and recreate the whole database:

```powershell
cd "Backend\IoTMonitoringSystem.API"
dotnet ef database drop --force --project ..\IoTMonitoringSystem.Infrastructure
dotnet ef database update --project ..\IoTMonitoringSystem.Infrastructure
```

Then start the API (migrations + admin seed). Only use if the SQL script fails due to permissions.
