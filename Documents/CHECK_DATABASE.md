# How to Check Database and Devices

## 📊 Database Information

**Database Name:** `IoTMonitoringDB`  
**Server:** `(localdb)\mssqllocaldb` (SQL Server LocalDB)  
**Table:** `Devices`

## 🔍 Method 1: Using SQL Server Management Studio (SSMS)

### Step 1: Connect to Database
1. Open SQL Server Management Studio (SSMS)
2. Connect to server: `(localdb)\mssqllocaldb`
3. Or use: `localhost` with Windows Authentication

### Step 2: View Database
1. Expand "Databases" in Object Explorer
2. Find `IoTMonitoringDB`
3. Expand it → Tables → `Devices`

### Step 3: Query Devices
Right-click on `Devices` table → "Select Top 1000 Rows"

Or run this SQL query:
```sql
SELECT * FROM Devices ORDER BY CreatedAt DESC;
```

## 🔍 Method 2: Using Command Line (sqlcmd)

### Connect to Database
```powershell
sqlcmd -S "(localdb)\mssqllocaldb" -d IoTMonitoringDB
```

### Query Devices
```sql
SELECT * FROM Devices;
GO
```

### Exit
```sql
EXIT
```

## 🔍 Method 3: Using PowerShell

### Check if database exists
```powershell
sqlcmd -S "(localdb)\mssqllocaldb" -Q "SELECT name FROM sys.databases WHERE name = 'IoTMonitoringDB'"
```

### Query all devices
```powershell
sqlcmd -S "(localdb)\mssqllocaldb" -d IoTMonitoringDB -Q "SELECT DeviceId, DeviceName, DeviceType, Location, Status, CreatedAt FROM Devices ORDER BY CreatedAt DESC"
```

### Query specific device
```powershell
sqlcmd -S "(localdb)\mssqllocaldb" -d IoTMonitoringDB -Q "SELECT * FROM Devices WHERE DeviceName LIKE '%Test%'"
```

## 🔍 Method 4: Using .NET Entity Framework

### Check database via EF Core
```powershell
cd Backend
dotnet ef dbcontext info --project IoTMonitoringSystem.Infrastructure/IoTMonitoringSystem.Infrastructure.csproj
```

### View all tables
```powershell
sqlcmd -S "(localdb)\mssqllocaldb" -d IoTMonitoringDB -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'"
```

## 📋 Device Table Structure

The `Devices` table has these columns:
- `DeviceId` (int, Primary Key)
- `DeviceName` (nvarchar)
- `DeviceType` (nvarchar)
- `Location` (nvarchar)
- `Status` (nvarchar)
- `FacilityType` (nvarchar, nullable)
- `EdgeDeviceType` (nvarchar, nullable)
- `EdgeDeviceId` (nvarchar, nullable)
- `Description` (nvarchar, nullable)
- `CreatedAt` (datetime2)
- `UpdatedAt` (datetime2, nullable)

## 🔍 Quick Query Examples

### Get all devices
```sql
SELECT * FROM Devices;
```

### Get devices by status
```sql
SELECT * FROM Devices WHERE Status = 'Active';
```

### Get recent devices
```sql
SELECT TOP 10 * FROM Devices ORDER BY CreatedAt DESC;
```

### Count devices
```sql
SELECT COUNT(*) AS TotalDevices FROM Devices;
```

### Get device with sensors
```sql
SELECT d.DeviceId, d.DeviceName, COUNT(s.SensorId) AS SensorCount
FROM Devices d
LEFT JOIN Sensors s ON d.DeviceId = s.DeviceId
GROUP BY d.DeviceId, d.DeviceName;
```

## 🛠️ Troubleshooting

### Database not found?
1. Check if migrations were applied:
   ```powershell
   cd Backend
   dotnet ef database update --project IoTMonitoringSystem.Infrastructure/IoTMonitoringSystem.Infrastructure.csproj
   ```

### Can't connect to LocalDB?
1. Start LocalDB:
   ```powershell
   sqllocaldb start mssqllocaldb
   ```

2. Check if it's running:
   ```powershell
   sqllocaldb info mssqllocaldb
   ```

### Connection string location
Check: `Backend/IoTMonitoringSystem.API/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=IoTMonitoringDB;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

## ✅ Verification Steps

1. **Check database exists:**
   ```powershell
   sqlcmd -S "(localdb)\mssqllocaldb" -Q "SELECT name FROM sys.databases WHERE name = 'IoTMonitoringDB'"
   ```

2. **Check Devices table exists:**
   ```powershell
   sqlcmd -S "(localdb)\mssqllocaldb" -d IoTMonitoringDB -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Devices'"
   ```

3. **Count devices:**
   ```powershell
   sqlcmd -S "(localdb)\mssqllocaldb" -d IoTMonitoringDB -Q "SELECT COUNT(*) AS DeviceCount FROM Devices"
   ```

4. **List all devices:**
   ```powershell
   sqlcmd -S "(localdb)\mssqllocaldb" -d IoTMonitoringDB -Q "SELECT DeviceId, DeviceName, DeviceType, Location, Status, CreatedAt FROM Devices"
   ```


