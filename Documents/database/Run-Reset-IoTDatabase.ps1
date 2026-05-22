# Reset local IoTMonitoringDB — empty tables, IDs restart at 1
# Run: Right-click -> Run with PowerShell, or:
#   cd "c:\Spring 2026\capstone_project\Documents\database"
#   .\Run-Reset-IoTDatabase.ps1

$ErrorActionPreference = "Stop"
$Server = "(localdb)\mssqllocaldb"
$Database = "IoTMonitoringDB"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SqlFile = Join-Path $ScriptDir "Reset-IoTDatabase.sql"

Write-Host "=== IoT database reset (local) ===" -ForegroundColor Cyan
Write-Host "Server: $Server"
Write-Host "Database: $Database"
Write-Host ""

$sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
if (-not $sqlcmd) {
    Write-Host "sqlcmd not found. Install SQL Server tools or run Reset-IoTDatabase.sql manually in SSMS." -ForegroundColor Red
    Write-Host "  Download: SQL Server Management Studio (SSMS)"
    exit 1
}

Write-Host "IMPORTANT: Stop the backend API (dotnet run) before continuing." -ForegroundColor Yellow
$confirm = Read-Host "Type YES to delete all data and reset IDs"
if ($confirm -ne "YES") {
    Write-Host "Cancelled."
    exit 0
}

Write-Host ""
Write-Host "Running $SqlFile ..." -ForegroundColor Cyan
& sqlcmd -S $Server -d $Database -E -i $SqlFile

if ($LASTEXITCODE -ne 0) {
    Write-Host "sqlcmd failed (exit $LASTEXITCODE). Is LocalDB running? Try: sqllocaldb start MSSQLLocalDB" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Verifying row counts..." -ForegroundColor Cyan
& sqlcmd -S $Server -d $Database -E -Q @"
SELECT 'Devices' AS [Table], COUNT(*) AS [Rows] FROM Devices
UNION ALL SELECT 'Sensors', COUNT(*) FROM Sensors
UNION ALL SELECT 'Users', COUNT(*) FROM Users;
"@ -W

Write-Host ""
Write-Host "Done. Next steps:" -ForegroundColor Green
Write-Host "  1. Start API:  cd Backend\IoTMonitoringSystem.API  ->  dotnet run"
Write-Host "  2. Login: admin / Admin@123"
Write-Host "  3. Add device 1, then sensors 1, 2, 3, 4 in the UI"
Write-Host "  4. Upload Arduino sketch (DEVICE_ID=1, sensors 1-4)"
