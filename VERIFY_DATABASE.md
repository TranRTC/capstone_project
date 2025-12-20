# Database Verification Guide

## ✅ Database EXISTS and is Working!

The database **WAS created automatically** when we ran the migration earlier. Here's how to verify:

## How the Database Was Created

When we ran this command earlier:
```powershell
dotnet ef database update
```

Entity Framework Core:
1. ✅ Created the database `IoTMonitoringDB` in SQL Server LocalDB
2. ✅ Created all 8 tables with proper structure
3. ✅ Set up all relationships and indexes
4. ✅ Applied the migration successfully

## Database Connection

The API is connected to the database via:
- **Server:** `(localdb)\MSSQLLocalDB` (SQL Server LocalDB)
- **Database:** `IoTMonitoringDB`
- **Connection String:** In `appsettings.json`

## Verify Database Exists

### Method 1: Using SQL Server Management Studio (SSMS)

1. Open SQL Server Management Studio
2. Connect to: `(localdb)\MSSQLLocalDB`
3. Expand "Databases"
4. You should see `IoTMonitoringDB`
5. Expand it to see all tables:
   - Devices
   - Sensors
   - SensorReadings
   - Alerts
   - AlertRules
   - DeviceStatusHistories
   - OperationalMetrics
   - DeviceConfigurations

### Method 2: Using Command Line (sqlcmd)

```powershell
# Check if database exists
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "SELECT name FROM sys.databases WHERE name = 'IoTMonitoringDB'"

# Connect to database and list tables
sqlcmd -S "(localdb)\MSSQLLocalDB" -d "IoTMonitoringDB" -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'"

# Check data in Devices table
sqlcmd -S "(localdb)\MSSQLLocalDB" -d "IoTMonitoringDB" -Q "SELECT * FROM Devices"

# Check data in Sensors table
sqlcmd -S "(localdb)\MSSQLLocalDB" -d "IoTMonitoringDB" -Q "SELECT * FROM Sensors"

# Check data in Alerts table
sqlcmd -S "(localdb)\MSSQLLocalDB" -d "IoTMonitoringDB" -Q "SELECT * FROM Alerts"

# Check data in SensorReadings table
sqlcmd -S "(localdb)\MSSQLLocalDB" -d "IoTMonitoringDB" -Q "SELECT * FROM SensorReadings"
```

### Method 3: Using Visual Studio

1. Open Visual Studio
2. Go to View → SQL Server Object Explorer
3. Expand `(localdb)\MSSQLLocalDB`
4. Expand Databases → `IoTMonitoringDB`
5. Expand Tables to see all tables
6. Right-click any table → "View Data" to see the data

## Proof the Database is Working

The test results prove the database is working because:

1. **Device Created** → Data saved to `Devices` table
2. **Sensor Created** → Data saved to `Sensors` table
3. **Alert Rule Created** → Data saved to `AlertRules` table
4. **Sensor Readings Created** → Data saved to `SensorReadings` table
5. **Alert Triggered** → Data saved to `Alerts` table
6. **Alert Acknowledged/Resolved** → Data updated in `Alerts` table

**If the database wasn't working, all these operations would have FAILED with connection errors!**

## Check Database Contents

After running the tests, you should see:

### Devices Table
- At least 1 device (Device ID: 1)
- Device Name: "Test Temperature Sensor"
- Location: "Test Lab - Room 101"

### Sensors Table
- At least 1 sensor (Sensor ID: 1)
- Sensor Name: "Temperature Sensor"
- Linked to Device ID: 1

### SensorReadings Table
- At least 2 readings
- Reading 1: Value = 25.0°C
- Reading 2: Value = 35.5°C

### Alerts Table
- At least 1 alert
- Alert triggered by reading with value 35.5°C
- Status: Resolved
- Acknowledged and Resolved timestamps

### AlertRules Table
- At least 1 alert rule
- Threshold: 30.0
- Operator: ">"
- Severity: "High"

## If Database Doesn't Exist

If for some reason the database doesn't exist, you can recreate it:

```powershell
# Navigate to project
cd "C:\Spring 2026\capstone_project"

# Remove existing migration (if needed)
dotnet ef migrations remove --project IoTMonitoringSystem.Infrastructure

# Create new migration
dotnet ef migrations add InitialCreate --project IoTMonitoringSystem.Infrastructure

# Create database and apply migration
dotnet ef database update --project IoTMonitoringSystem.Infrastructure
```

## Database Location

SQL Server LocalDB stores databases in:
```
C:\Users\<YourUsername>\AppData\Local\Microsoft\Microsoft SQL Server Local DB\Instances\MSSQLLocalDB\
```

The database files are:
- `IoTMonitoringDB.mdf` (data file)
- `IoTMonitoringDB_log.ldf` (log file)

## Summary

✅ **Database EXISTS** - Created by Entity Framework migration  
✅ **Database is CONNECTED** - API successfully connects and queries  
✅ **Database has DATA** - All test operations saved data successfully  
✅ **Database is WORKING** - All CRUD operations completed successfully  

The fact that all tests passed **proves** the database is working correctly!

