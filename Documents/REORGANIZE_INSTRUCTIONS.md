# Project Reorganization Instructions

## ⚠️ Important: Stop Running Processes First!

Before moving files, you need to stop:
1. **Frontend server** (npm start) - Press `Ctrl+C` in that terminal
2. **Backend API** (dotnet run) - Press `Ctrl+C` in that terminal
3. **Any open files** in Visual Studio or other editors

## 📁 Target Structure

```
capstone_project/
├── Backend/              # All .NET projects
│   ├── IoTMonitoringSystem.API/
│   ├── IoTMonitoringSystem.Core/
│   ├── IoTMonitoringSystem.Infrastructure/
│   ├── IoTMonitoringSystem.Services/
│   └── IoTMonitoringSystem.slnx
│
├── Frontend/             # React application
│   └── iot-monitoring-frontend/
│
└── Documents/            # All documentation
    ├── context/          # Design documents
    └── *.md, *.ps1       # Guides and scripts
```

## 🔄 Manual Move Steps

### Step 1: Stop All Processes
- Stop frontend: `Ctrl+C` in npm start terminal
- Stop backend: `Ctrl+C` in dotnet run terminal
- Close Visual Studio/editors

### Step 2: Move Remaining Files

**Move API Project:**
```powershell
cd "C:\Spring 2026\capstone_project"
Move-Item -Path "IoTMonitoringSystem.API" -Destination "Backend\" -Force
```

**Move Frontend:**
```powershell
Move-Item -Path "iot-monitoring-frontend" -Destination "Frontend\" -Force
```

### Step 3: Update Solution File

The solution file needs to be updated to reflect new paths. You can either:

**Option A: Recreate solution (Recommended)**
```powershell
cd Backend
dotnet new sln -n IoTMonitoringSystem
dotnet sln add IoTMonitoringSystem.API/IoTMonitoringSystem.API.csproj
dotnet sln add IoTMonitoringSystem.Core/IoTMonitoringSystem.Core.csproj
dotnet sln add IoTMonitoringSystem.Infrastructure/IoTMonitoringSystem.Infrastructure.csproj
dotnet sln add IoTMonitoringSystem.Services/IoTMonitoringSystem.Services.csproj
```

**Option B: Edit solution file manually**
- Open `Backend/IoTMonitoringSystem.slnx` in a text editor
- Update all paths to remove `../` prefixes

### Step 4: Verify

**Test Backend:**
```powershell
cd Backend
dotnet build
dotnet run --project IoTMonitoringSystem.API/IoTMonitoringSystem.API.csproj
```

**Test Frontend:**
```powershell
cd Frontend/iot-monitoring-frontend
npm start
```

## ✅ Current Status

**Already Moved:**
- ✅ Core, Infrastructure, Services projects → `Backend/`
- ✅ All documentation → `Documents/`
- ✅ Solution file → `Backend/`

**Still Need to Move:**
- ⏳ `IoTMonitoringSystem.API/` → `Backend/` (locked by running process)
- ⏳ `iot-monitoring-frontend/` → `Frontend/` (locked by running process)

## 🚀 Quick Script

After stopping processes, run this:

```powershell
cd "C:\Spring 2026\capstone_project"

# Move API project
if (Test-Path "IoTMonitoringSystem.API") {
    Move-Item -Path "IoTMonitoringSystem.API" -Destination "Backend\" -Force
    Write-Host "✅ API moved" -ForegroundColor Green
}

# Move Frontend
if (Test-Path "iot-monitoring-frontend") {
    Move-Item -Path "iot-monitoring-frontend" -Destination "Frontend\" -Force
    Write-Host "✅ Frontend moved" -ForegroundColor Green
}

# Recreate solution
cd Backend
dotnet new sln -n IoTMonitoringSystem -f
dotnet sln add IoTMonitoringSystem.API/IoTMonitoringSystem.API.csproj
dotnet sln add IoTMonitoringSystem.Core/IoTMonitoringSystem.Core.csproj
dotnet sln add IoTMonitoringSystem.Infrastructure/IoTMonitoringSystem.Infrastructure.csproj
dotnet sln add IoTMonitoringSystem.Services/IoTMonitoringSystem.Services.csproj
Remove-Item "IoTMonitoringSystem.slnx" -ErrorAction SilentlyContinue
Write-Host "✅ Solution recreated" -ForegroundColor Green
```

## 📝 Updated Commands

After reorganization, use these paths:

**Backend:**
```powershell
cd Backend
dotnet run --project IoTMonitoringSystem.API/IoTMonitoringSystem.API.csproj
```

**Frontend:**
```powershell
cd Frontend/iot-monitoring-frontend
npm start
```

**Database Migration:**
```powershell
cd Backend
dotnet ef database update --project IoTMonitoringSystem.Infrastructure/IoTMonitoringSystem.Infrastructure.csproj
```

