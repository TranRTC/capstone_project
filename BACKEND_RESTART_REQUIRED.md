# ⚠️ BACKEND RESTART REQUIRED

## The Problem
You're still getting **307 redirects**, which means the backend is redirecting HTTP requests to HTTPS. This breaks CORS.

## The Solution
**You MUST restart the backend using the HTTP profile ONLY.**

### Step 1: Stop the Current Backend
Press `Ctrl+C` in the terminal where the backend is running.

### Step 2: Restart with HTTP Profile
Run this command:

```powershell
cd Backend
dotnet run --project IoTMonitoringSystem.API/IoTMonitoringSystem.API.csproj --launch-profile http
```

### Step 3: Verify It's Running on HTTP Only
You should see:
```
Now listening on: http://localhost:5000
```

**DO NOT see:**
```
Now listening on: https://localhost:5001;http://localhost:5000
```

If you see both ports, the backend is still using the HTTPS profile and will redirect HTTP to HTTPS.

### Step 4: Test the Backend
Open in browser: http://localhost:5000/swagger

If Swagger loads, the backend is running correctly.

### Step 5: Refresh Frontend
Hard refresh the frontend (Ctrl+F5) and check the console. CORS errors should be gone.

## Why This Happens
- The "https" profile listens on both HTTP (5000) and HTTPS (5001)
- When you access HTTP, it redirects to HTTPS (307 redirect)
- CORS preflight requests CANNOT follow redirects
- This breaks all API calls

## The Fix Applied
1. ✅ Added custom OPTIONS handler (handles CORS preflight immediately)
2. ✅ CORS middleware placed first in pipeline
3. ✅ HTTPS redirection disabled in development
4. ✅ Kestrel configured to listen on HTTP only in development

But you **MUST restart the backend** with the HTTP profile for it to work!

