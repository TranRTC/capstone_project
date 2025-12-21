# Running Backend and Frontend

## 🚀 Quick Start

### Backend (API)
```powershell
cd Backend
dotnet run --project IoTMonitoringSystem.API/IoTMonitoringSystem.API.csproj
```

**URLs:**
- API: http://localhost:5286
- Swagger UI: http://localhost:5286/swagger

### Frontend (React)
```powershell
cd Frontend/iot-monitoring-frontend
npm start
```

**URL:**
- Frontend: http://localhost:3000

## ✅ Verification

### Check Backend
1. Look for: `Now listening on: http://localhost:5286`
2. Open: http://localhost:5286/swagger
3. Should see Swagger UI with API endpoints

### Check Frontend
1. Look for: `Compiled successfully!`
2. Open: http://localhost:3000
3. Should see "IoT Monitoring System" interface

## 🔗 Connection

The frontend is configured to connect to:
- API Base URL: `http://localhost:5286/api/v1`
- SignalR Hub: `http://localhost:5286/monitoringhub`

## ⚠️ Troubleshooting

### Backend not starting?
- Check if port 5286 is already in use
- Verify database connection string in `appsettings.json`
- Check for compilation errors in the terminal

### Frontend not starting?
- Check if port 3000 is already in use
- Verify Node.js is installed: `node --version`
- Check for compilation errors in the terminal
- Try: `npm install --legacy-peer-deps` if dependencies are missing

### Frontend can't connect to backend?
- Verify backend is running first
- Check CORS settings in `Backend/IoTMonitoringSystem.API/Program.cs`
- Check browser console (F12) for errors

## 📝 Notes

- Backend takes ~5-10 seconds to start
- Frontend takes ~30-60 seconds for first compilation
- Both services run in separate terminal windows
- Keep both terminals open while developing

