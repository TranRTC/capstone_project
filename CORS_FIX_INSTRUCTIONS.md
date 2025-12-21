# CORS Fix Instructions

## Problem
The frontend cannot connect to the backend due to CORS errors and 307 redirects.

## Solution Applied
1. ✅ CORS middleware moved to the FIRST position in the pipeline
2. ✅ HTTPS redirection disabled in development
3. ✅ CORS configured for both HTTP and HTTPS origins

## IMPORTANT: How to Run the Backend

**You MUST run the backend using the HTTP profile (not HTTPS) to avoid 307 redirects:**

```powershell
cd Backend
dotnet run --project IoTMonitoringSystem.API/IoTMonitoringSystem.API.csproj --launch-profile http
```

Or simply:
```powershell
cd Backend
dotnet run --project IoTMonitoringSystem.API/IoTMonitoringSystem.API.csproj
```

**Make sure it shows:**
```
Now listening on: http://localhost:5000
```

**NOT:**
```
Now listening on: https://localhost:5001;http://localhost:5000
```

## After Restarting Backend

1. **Restart the backend** with the HTTP profile
2. **Refresh the frontend** (hard refresh: Ctrl+F5)
3. **Check the browser console** - CORS errors should be gone
4. **Verify devices load** - You should see your 2 devices from the database

## If Still Having Issues

1. Check backend is running: Open http://localhost:5000/swagger in browser
2. Check CORS headers: Use browser DevTools Network tab, look for `Access-Control-Allow-Origin` header
3. Clear browser cache and hard refresh (Ctrl+F5)

