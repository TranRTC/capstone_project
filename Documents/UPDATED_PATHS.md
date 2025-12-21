# Updated Paths After Reorganization

## 📁 New Project Structure

```
capstone_project/
├── Backend/                    # All backend code
│   ├── IoTMonitoringSystem.API/
│   ├── IoTMonitoringSystem.Core/
│   ├── IoTMonitoringSystem.Infrastructure/
│   ├── IoTMonitoringSystem.Services/
│   └── IoTMonitoringSystem.slnx
│
├── Frontend/                   # All frontend code
│   └── iot-monitoring-frontend/
│
└── Documents/                  # All documentation
    ├── context/               # Design documents
    └── *.md, *.ps1, *.html    # Guides and scripts
```

## 🔄 Updated Commands

### Backend Commands

**Old path:**
```powershell
cd "C:\Spring 2026\capstone_project"
dotnet run --project IoTMonitoringSystem.API/IoTMonitoringSystem.API.csproj
```

**New path:**
```powershell
cd "C:\Spring 2026\capstone_project\Backend"
dotnet run --project IoTMonitoringSystem.API/IoTMonitoringSystem.API.csproj
```

### Frontend Commands

**Old path:**
```powershell
cd "C:\Spring 2026\capstone_project\iot-monitoring-frontend"
npm start
```

**New path:**
```powershell
cd "C:\Spring 2026\capstone_project\Frontend\iot-monitoring-frontend"
npm start
```

### Database Migration

**Old path:**
```powershell
dotnet ef database update --project IoTMonitoringSystem.Infrastructure
```

**New path:**
```powershell
cd Backend
dotnet ef database update --project IoTMonitoringSystem.Infrastructure/IoTMonitoringSystem.Infrastructure.csproj
```

## 📝 Updated Script Paths

If you have any scripts, update them to use the new paths:

- Backend: `Backend/IoTMonitoringSystem.API/`
- Frontend: `Frontend/iot-monitoring-frontend/`
- Documents: `Documents/`

## ✅ Benefits

1. **Clear Separation** - Backend and frontend are in separate folders
2. **Better Organization** - All documentation in one place
3. **Professional Structure** - Follows industry standards
4. **Easy Navigation** - Everything is logically organized
5. **Cleaner Root** - Project root is not cluttered

