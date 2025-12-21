# Testing Frontend-Backend Connection

## 🎯 Goal
Verify that the frontend can successfully communicate with the backend by adding a new device.

## ✅ Step-by-Step Test

### Step 1: Verify Services Are Running

**Backend:**
- Open: http://localhost:5286/swagger
- You should see the Swagger UI with API endpoints
- If not running, start with:
  ```powershell
  cd Backend
  dotnet run --project IoTMonitoringSystem.API/IoTMonitoringSystem.API.csproj
  ```

**Frontend:**
- Open: http://localhost:3000
- You should see the IoT Monitoring System dashboard
- If not running, start with:
  ```powershell
  cd Frontend/iot-monitoring-frontend
  npm start
  ```

### Step 2: Add a Device via Frontend

1. **Navigate to Devices Page:**
   - Click "DEVICES" in the navigation bar
   - Or go directly to: http://localhost:3000/devices

2. **Click "Add Device" Button:**
   - You should see a form to create a new device

3. **Fill in Device Information:**
   - **Device Name:** e.g., "Test Device 1"
   - **Device Type:** e.g., "Sensor Hub"
   - **Location:** e.g., "Building A, Room 101"
   - **Status:** Select "Active" or "Inactive"
   - **Description:** (Optional) e.g., "Test device for connection verification"

4. **Click "Save" or "Create Device"**

### Step 3: Verify Device Was Created

**Option A: Check Frontend (Easiest)**
- After clicking Save, the device should appear in the devices list
- You should see your new device in the table
- The page should refresh or show a success message

**Option B: Check Backend API (Swagger)**
1. Open: http://localhost:5286/swagger
2. Find the `GET /api/v1/Devices` endpoint
3. Click "Try it out"
4. Click "Execute"
5. Check the response - your new device should be in the list

**Option C: Check Backend API (Direct URL)**
- Open: http://localhost:5286/api/v1/Devices
- You should see JSON response with all devices including your new one

**Option D: Check Database**
```powershell
# Using SQL Server Management Studio or command line
# Connect to: (localdb)\mssqllocaldb
# Database: IoTMonitoringDB
# Table: Devices
# Query: SELECT * FROM Devices ORDER BY CreatedAt DESC
```

### Step 4: Check Browser Console

1. Open browser Developer Tools (F12)
2. Go to **Console** tab
3. Look for:
   - ✅ Success messages: "Device created successfully"
   - ✅ API calls: "POST http://localhost:5286/api/v1/Devices"
   - ❌ Errors: CORS errors, 404, 500, etc.

4. Go to **Network** tab
5. Filter by "XHR" or "Fetch"
6. Look for the POST request to `/api/v1/Devices`
7. Check:
   - Status: Should be 200 or 201 (Created)
   - Request Payload: Your device data
   - Response: The created device object

## 🔍 What to Look For

### ✅ Success Indicators:
- Device appears in the devices list
- No errors in browser console
- Network request shows status 200/201
- Success message appears (if implemented)
- Device visible in Swagger API response

### ❌ Failure Indicators:
- Error message in browser
- Console shows CORS errors
- Network request shows 404, 500, or other errors
- Device doesn't appear in list
- "Failed to fetch" or "Network error" messages

## 🐛 Troubleshooting

### CORS Errors
**Error:** "Access to fetch at 'http://localhost:5286' from origin 'http://localhost:3000' has been blocked by CORS policy"

**Solution:**
- Check `Backend/IoTMonitoringSystem.API/Program.cs`
- Ensure CORS is configured for `http://localhost:3000`
- Restart backend after changes

### 404 Not Found
**Error:** "POST http://localhost:5286/api/v1/Devices 404"

**Solution:**
- Verify backend is running on port 5286
- Check API route in `DevicesController.cs`
- Ensure route is `/api/v1/Devices`

### 500 Internal Server Error
**Error:** "POST http://localhost:5286/api/v1/Devices 500"

**Solution:**
- Check backend terminal for error messages
- Verify database connection
- Check backend logs

### Connection Refused
**Error:** "Failed to fetch" or "ERR_CONNECTION_REFUSED"

**Solution:**
- Ensure backend is running
- Check if port 5286 is correct
- Verify firewall isn't blocking

## 📝 Quick Test Script

You can also test the connection directly using PowerShell:

```powershell
# Test backend is accessible
Invoke-RestMethod -Uri "http://localhost:5286/api/v1/Devices" -Method Get

# Create a test device
$device = @{
    name = "Test Device"
    deviceType = "Sensor Hub"
    location = "Test Location"
    status = "Active"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5286/api/v1/Devices" -Method Post -Body $device -ContentType "application/json"
```

## ✅ Expected Result

After adding a device:
1. ✅ Device appears in frontend devices list
2. ✅ Device is visible in Swagger API
3. ✅ Device is saved in database
4. ✅ No errors in console
5. ✅ Network request succeeds (200/201)

If all these are true, your frontend is successfully connected to the backend! 🎉

