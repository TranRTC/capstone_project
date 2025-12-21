# Project Structure

## 📁 Organization

The project is organized into three main folders:

### Backend/
Contains all ASP.NET Core backend projects:
- `IoTMonitoringSystem.API/` - Main Web API project
- `IoTMonitoringSystem.Core/` - Entity classes and DTOs
- `IoTMonitoringSystem.Infrastructure/` - Data access layer
- `IoTMonitoringSystem.Services/` - Business logic layer
- `IoTMonitoringSystem.slnx` - Solution file

**To run backend:**
```powershell
cd Backend
dotnet run --project IoTMonitoringSystem.API/IoTMonitoringSystem.API.csproj
```

### Frontend/
Contains the React frontend application:
- `iot-monitoring-frontend/` - React TypeScript application

**To run frontend:**
```powershell
cd Frontend/iot-monitoring-frontend
npm install --legacy-peer-deps
npm start
```

### Documents/
Contains all project documentation:
- `context/` - Design documents (001-010)
  - 001_Overview.md
  - 002_Requirements.md
  - 003_DatabaseDesign.md
  - 004_ApplicationDesign.md
  - 005_FrontendDesign.md
  - 006_APIDocumentation.md
  - 007_TestingPlan.md
  - 008_DeploymentGuide.md
  - 009_ImplementationGuide.md
  - 010_UserManual.md
- Guides and instructions (*.md files)
- Scripts (*.ps1 files)

## 🎯 Benefits of This Structure

1. **Clear Separation** - Backend and frontend are clearly separated
2. **Easy Navigation** - Everything is organized logically
3. **Better Collaboration** - Team members know where to find things
4. **Cleaner Root** - Project root is not cluttered
5. **Professional** - Follows industry best practices

## 📝 File Locations

### Backend Files
- Solution: `Backend/IoTMonitoringSystem.slnx`
- API Project: `Backend/IoTMonitoringSystem.API/`
- Database: SQL Server LocalDB (IoTMonitoringDB)

### Frontend Files
- React App: `Frontend/iot-monitoring-frontend/`
- Package.json: `Frontend/iot-monitoring-frontend/package.json`

### Documentation
- All docs: `Documents/`
- Design docs: `Documents/context/`

## 🔄 Updated Paths

After reorganization, update any scripts or commands that reference:
- Old: `IoTMonitoringSystem.API/` → New: `Backend/IoTMonitoringSystem.API/`
- Old: `iot-monitoring-frontend/` → New: `Frontend/iot-monitoring-frontend/`
- Old: `context/` → New: `Documents/context/`

