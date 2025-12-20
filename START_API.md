# How to Start the API

## Step-by-Step Instructions

### Method 1: Using Command Line (Recommended)

1. **Open PowerShell or Command Prompt**
   - Press `Windows Key + X`
   - Select "Windows PowerShell" or "Terminal"
   - Or search for "PowerShell" in the Start menu

2. **Navigate to the Project Directory**
   ```powershell
   cd "C:\Spring 2026\capstone_project"
   ```

3. **Start the API**
   ```powershell
   dotnet run --project IoTMonitoringSystem.API/IoTMonitoringSystem.API.csproj
   ```

4. **Wait for the API to Start**
   You should see output like:
   ```
   Building...
   info: Microsoft.Hosting.Lifetime[14]
         Now listening on: http://localhost:5000
   info: Microsoft.Hosting.Lifetime[14]
         Now listening on: https://localhost:5001
   info: Microsoft.Hosting.Lifetime[0]
         Application started. Press Ctrl+C to shut down.
   ```

5. **Keep This Window Open**
   - The API will keep running as long as this window is open
   - Don't close this window while testing
   - To stop the API, press `Ctrl+C` in this window

---

### Method 2: Using Visual Studio

1. **Open Visual Studio**
2. **Open the Solution**
   - File → Open → Project/Solution
   - Navigate to: `C:\Spring 2026\capstone_project\IoTMonitoringSystem.sln`
   - Click "Open"

3. **Set Startup Project**
   - Right-click on `IoTMonitoringSystem.API` in Solution Explorer
   - Select "Set as Startup Project"

4. **Run the API**
   - Press `F5` to run with debugging
   - Or press `Ctrl+F5` to run without debugging

5. **View Output**
   - The API will start and show the listening URLs in the Output window

---

### Method 3: Using Visual Studio Code

1. **Open Visual Studio Code**
2. **Open the Folder**
   - File → Open Folder
   - Select: `C:\Spring 2026\capstone_project`

3. **Open Terminal**
   - View → Terminal (or press `` Ctrl+` ``)

4. **Run the Command**
   ```powershell
   dotnet run --project IoTMonitoringSystem.API/IoTMonitoringSystem.API.csproj
   ```

---

## Verify the API is Running

### Check 1: Look at the Console Output
You should see:
```
Now listening on: http://localhost:5000
Now listening on: https://localhost:5001
```

### Check 2: Open Swagger UI in Browser
1. Open your web browser
2. Navigate to: `http://localhost:5000/swagger`
3. You should see the Swagger API documentation page

### Check 3: Test with PowerShell
Open a **new** PowerShell window (keep the API running) and run:
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/v1/devices" -Method Get
```

If you get a response (even an empty list), the API is working!

---

## Common Issues and Solutions

### Issue 1: "Port already in use"
**Error:** `Failed to bind to address http://localhost:5000`

**Solution:**
- Find what's using the port:
  ```powershell
  netstat -ano | findstr :5000
  ```
- Kill the process (replace PID with the number from above):
  ```powershell
  taskkill /PID <PID> /F
  ```
- Or use a different port:
  ```powershell
  dotnet run --project IoTMonitoringSystem.API/IoTMonitoringSystem.API.csproj --urls "http://localhost:5002"
  ```

### Issue 2: "Database connection failed"
**Error:** `Cannot open database "IoTMonitoringDB"`

**Solution:**
1. Check if SQL Server LocalDB is running
2. Verify connection string in `appsettings.json`
3. Apply migrations:
   ```powershell
   dotnet ef database update --project IoTMonitoringSystem.Infrastructure
   ```

### Issue 3: "Build failed"
**Error:** Compilation errors

**Solution:**
1. Restore packages:
   ```powershell
   dotnet restore
   ```
2. Build the solution:
   ```powershell
   dotnet build
   ```
3. Fix any errors shown

### Issue 4: "Cannot find the project file"
**Error:** `The project file could not be found`

**Solution:**
- Make sure you're in the correct directory:
  ```powershell
  cd "C:\Spring 2026\capstone_project"
  ```
- Verify the project exists:
  ```powershell
  Test-Path "IoTMonitoringSystem.API\IoTMonitoringSystem.API.csproj"
  ```

---

## Quick Start Command (Copy & Paste)

```powershell
cd "C:\Spring 2026\capstone_project"
dotnet run --project IoTMonitoringSystem.API/IoTMonitoringSystem.API.csproj
```

---

## What to Do After Starting

Once the API is running:

1. **Open Swagger UI**: `http://localhost:5000/swagger`
2. **Run Test Script**: Open a new terminal and run `.\test-api.ps1`
3. **Test SignalR**: Open `test-signalr.html` in your browser

---

## Stopping the API

To stop the API:
- Press `Ctrl+C` in the terminal where it's running
- Or close the terminal window

---

## Next Steps

After the API is running:
- ✅ API is accessible at `http://localhost:5000`
- ✅ Swagger UI available at `http://localhost:5000/swagger`
- ✅ SignalR Hub available at `http://localhost:5000/monitoringhub`
- ✅ Ready to test endpoints!

