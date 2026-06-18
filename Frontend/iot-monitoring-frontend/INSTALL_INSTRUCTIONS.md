# Installation Instructions

## Prerequisites

1. **Install Node.js**
   - Download from: https://nodejs.org/
   - Install the LTS version
   - Verify installation:
     ```powershell
     node --version
     npm --version
     ```

## Installation Steps

1. **Navigate to frontend directory:**
   ```powershell
   cd iot-monitoring-frontend
   ```

2. **Install dependencies:**
   ```powershell
   npm install
   ```
   This will install all required packages including:
   - React and React DOM
   - Material-UI components
   - SignalR client
   - Axios for API calls
   - React Router for navigation
   - Recharts for data visualization

3. **Start the development server:**
   ```powershell
   npm start
   ```
   The app will open at http://localhost:3000

## Important Notes

- **Backend API must be running** on http://localhost:5000
- The frontend reads API and SignalR URLs from `src/config/runtimeConfig.ts` (env vars `REACT_APP_API_BASE_URL` and `REACT_APP_SIGNALR_HUB_URL` override defaults)

## Troubleshooting

### Port 3000 already in use
```powershell
# Use a different port
set PORT=3001
npm start
```

### Cannot connect to API
- Verify backend API is running: http://localhost:5000/swagger
- Check CORS settings in backend `Program.cs`
- Verify API URL in `src/config/runtimeConfig.ts` or set `REACT_APP_API_BASE_URL`

### Module not found errors
```powershell
# Delete node_modules and reinstall
rm -r node_modules
npm install
```

## Next Steps

After installation:
1. The app will automatically open in your browser
2. You should see the Dashboard
3. Navigate to Devices, Sensors, and Alerts pages
4. Real-time updates will work if SignalR is connected

