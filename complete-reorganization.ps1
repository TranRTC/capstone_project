# Complete Project Reorganization Script
# Run this AFTER stopping all running processes (npm start, dotnet run)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Project Reorganization Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if processes are running
Write-Host "Checking for running processes..." -ForegroundColor Yellow
$nodeProcesses = Get-Process -Name "node" -ErrorAction SilentlyContinue
$dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue

if ($nodeProcesses -or $dotnetProcesses) {
    Write-Host "⚠️  WARNING: Found running processes!" -ForegroundColor Red
    Write-Host "Please stop:" -ForegroundColor Yellow
    if ($nodeProcesses) { Write-Host "  • Node.js processes (npm start)" -ForegroundColor White }
    if ($dotnetProcesses) { Write-Host "  • .NET processes (dotnet run)" -ForegroundColor White }
    Write-Host "`nPress Ctrl+C in those terminals, then run this script again." -ForegroundColor Yellow
    Read-Host "Press Enter to continue anyway (files may be locked)"
}

cd "C:\Spring 2026\capstone_project"

# Create folders if they don't exist
Write-Host "`nCreating folder structure..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path "Backend", "Frontend", "Documents" -Force | Out-Null
Write-Host "✅ Folders created" -ForegroundColor Green

# Move API project
Write-Host "`nMoving API project..." -ForegroundColor Yellow
if (Test-Path "IoTMonitoringSystem.API") {
    try {
        Move-Item -Path "IoTMonitoringSystem.API" -Destination "Backend\" -Force -ErrorAction Stop
        Write-Host "✅ API project moved" -ForegroundColor Green
    } catch {
        Write-Host "❌ Failed to move API project: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "   Make sure no processes are using these files!" -ForegroundColor Yellow
    }
} else {
    Write-Host "⚠️  API project not found (may already be moved)" -ForegroundColor Yellow
}

# Move Frontend
Write-Host "`nMoving Frontend..." -ForegroundColor Yellow
if (Test-Path "iot-monitoring-frontend") {
    try {
        Move-Item -Path "iot-monitoring-frontend" -Destination "Frontend\" -Force -ErrorAction Stop
        Write-Host "✅ Frontend moved" -ForegroundColor Green
    } catch {
        Write-Host "❌ Failed to move Frontend: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "   Make sure no processes are using these files!" -ForegroundColor Yellow
    }
} else {
    Write-Host "⚠️  Frontend not found (may already be moved)" -ForegroundColor Yellow
}

# Recreate solution file
Write-Host "`nRecreating solution file..." -ForegroundColor Yellow
cd Backend
if (Test-Path "IoTMonitoringSystem.slnx") {
    Remove-Item "IoTMonitoringSystem.slnx" -Force -ErrorAction SilentlyContinue
}

try {
    dotnet new sln -n IoTMonitoringSystem -f | Out-Null
    dotnet sln add IoTMonitoringSystem.API/IoTMonitoringSystem.API.csproj 2>&1 | Out-Null
    dotnet sln add IoTMonitoringSystem.Core/IoTMonitoringSystem.Core.csproj 2>&1 | Out-Null
    dotnet sln add IoTMonitoringSystem.Infrastructure/IoTMonitoringSystem.Infrastructure.csproj 2>&1 | Out-Null
    dotnet sln add IoTMonitoringSystem.Services/IoTMonitoringSystem.Services.csproj 2>&1 | Out-Null
    Write-Host "✅ Solution file recreated" -ForegroundColor Green
} catch {
    Write-Host "⚠️  Could not recreate solution: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "   You may need to add projects manually in Visual Studio" -ForegroundColor Yellow
}

# Verify structure
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Verification" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "`nBackend folder:" -ForegroundColor Yellow
Get-ChildItem "Backend" -Directory | ForEach-Object { Write-Host "  ✅ $($_.Name)" -ForegroundColor Green }

Write-Host "`nFrontend folder:" -ForegroundColor Yellow
Get-ChildItem "Frontend" -Directory | ForEach-Object { Write-Host "  ✅ $($_.Name)" -ForegroundColor Green }

Write-Host "`nDocuments folder:" -ForegroundColor Yellow
Write-Host "  ✅ context/ (design documents)" -ForegroundColor Green
Write-Host "  ✅ Guides and scripts" -ForegroundColor Green

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "  ✅ Reorganization Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "`nUpdated commands:" -ForegroundColor Yellow
Write-Host "  Backend:  cd Backend; dotnet run --project IoTMonitoringSystem.API/..." -ForegroundColor Cyan
Write-Host "  Frontend: cd Frontend/iot-monitoring-frontend; npm start" -ForegroundColor Cyan
Write-Host ""

