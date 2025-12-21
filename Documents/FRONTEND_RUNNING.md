# ✅ Frontend is Running!

## Status

The React frontend development server should now be starting!

## What to Expect

1. **Development Server Starts**
   - You'll see build output in the terminal
   - The server compiles the React app
   - This takes 30-60 seconds the first time

2. **Browser Opens Automatically**
   - The app will open at: **http://localhost:3000**
   - If it doesn't open automatically, go to that URL manually

3. **What You'll See**
   - "IoT Monitoring System" header
   - Navigation bar with: Dashboard, Devices, Sensors, Alerts
   - Dashboard page with statistics
   - Real-time connection status

## Important Notes

### Backend Must Be Running
- Make sure your backend API is running on **http://localhost:5286**
- If backend is not running, the frontend won't be able to fetch data
- Start backend in a separate terminal:
  ```powershell
  cd "C:\Spring 2026\capstone_project"
  dotnet run --project IoTMonitoringSystem.API/IoTMonitoringSystem.API.csproj
  ```

### Port 3000
- Frontend runs on: **http://localhost:3000**
- If port 3000 is busy, React will ask to use a different port (like 3001)

## Testing the Frontend

### 1. Check Dashboard
- Navigate to Dashboard (home page)
- You should see:
  - Total Devices count
  - Active Devices count
  - Active Alerts count
  - Latest Reading
  - SignalR connection status

### 2. Test Device Management
- Click "Devices" in navigation
- Click "Add Device" button
- Fill in the form and create a device
- You should see it appear in the table

### 3. Test Sensors
- Click "Sensors" in navigation
- Select a device from dropdown
- View sensors for that device

### 4. Test Alerts
- Click "Alerts" in navigation
- View active alerts
- Test acknowledge/resolve buttons

### 5. Test Real-Time Updates
- Keep Dashboard open
- Create a sensor reading via Swagger or test script
- Watch the dashboard update in real-time!

## Troubleshooting

### "Cannot connect to API"
- Check backend is running: http://localhost:5286/swagger
- Verify API URL in `src/services/api.ts`
- Check CORS settings in backend

### "SignalR connection failed"
- Verify backend SignalR hub is running
- Check hub URL in `src/services/signalRService.ts`
- Make sure backend CORS allows http://localhost:3000

### "Page not loading"
- Check browser console (F12) for errors
- Verify all dependencies installed: `npm install --legacy-peer-deps`
- Try clearing browser cache

### "Port 3000 already in use"
- Close other applications using port 3000
- Or React will automatically use port 3001

## Stopping the Frontend

- Press `Ctrl+C` in the terminal where `npm start` is running
- Or close the terminal window

## Next Steps

Once the frontend is running:
1. ✅ Test all pages
2. ✅ Create devices and sensors
3. ✅ Test real-time updates
4. ✅ Verify alerts work
5. ✅ Test data visualization

**Enjoy your fully functional IoT Monitoring System!** 🎉

