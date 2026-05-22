# Shows which SQL Server instances have IoTMonitoringDB and what data is inside.
# Run this AFTER a reset to confirm you cleared the same DB the API uses.

$ErrorActionPreference = "Continue"
$DbName = "IoTMonitoringDB"

$servers = @(
    "(localdb)\mssqllocaldb",
    "localhost",
    "localhost\SQLEXPRESS",
    ".\SQLEXPRESS"
)

Write-Host "=== IoT database finder ===" -ForegroundColor Cyan
Write-Host "Your API uses (appsettings.json): Server=(localdb)\mssqllocaldb; Database=$DbName"
Write-Host ""

$sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
if (-not $sqlcmd) {
    Write-Host "sqlcmd not found. In SSMS run:" -ForegroundColor Yellow
    Write-Host "  SELECT @@SERVERNAME, DB_NAME(), (SELECT COUNT(*) FROM Devices) AS Devices;"
    exit 1
}

foreach ($server in $servers) {
    Write-Host "--- Server: $server ---" -ForegroundColor Yellow
    $q = @"
IF DB_ID('$DbName') IS NULL
  SELECT 'NO DATABASE' AS Status, '$DbName' AS DbName;
ELSE
BEGIN
  USE [$DbName];
  SELECT @@SERVERNAME AS ServerName, DB_NAME() AS DbName;
  SELECT DeviceId, DeviceName FROM Devices ORDER BY DeviceId;
  SELECT SensorId, DeviceId, SensorName FROM Sensors ORDER BY SensorId;
  SELECT IDENT_CURRENT('Devices') AS NextDeviceId, IDENT_CURRENT('Sensors') AS NextSensorId;
  SELECT 'Devices' AS T, COUNT(*) AS C FROM Devices
  UNION ALL SELECT 'Sensors', COUNT(*) FROM Sensors
  UNION ALL SELECT 'SensorReadings', COUNT(*) FROM SensorReadings;
END
"@
    & sqlcmd -S $server -E -Q $q -W 2>&1
    Write-Host ""
}

Write-Host "=== Which site are you using? ===" -ForegroundColor Cyan
Write-Host "  LOCAL dashboard:  http://localhost:3000  -> API http://localhost:5000  -> (localdb)"
Write-Host "  CLOUD dashboard:  Azure Static Web URL   -> Azure API            -> Azure SQL (separate!)"
Write-Host ""
Write-Host "Reset script only clears the database on the server where you ran it."
Write-Host "Cloud data is NOT cleared by resetting LocalDB."
