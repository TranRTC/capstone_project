# Fixing Blank Page Issue

## Current Status

The frontend has been set up but shows a blank page. Here's what to check:

## Step-by-Step Diagnosis

### 1. Check the Terminal Window

Look at the PowerShell window where `npm start` is running. You should see:

**✅ Good signs:**
- "Compiling..."
- "Compiled successfully!"
- "webpack compiled"
- "Local: http://localhost:3000"

**❌ Bad signs:**
- "Failed to compile"
- Red error messages
- "Module not found"
- TypeScript errors

### 2. Check Browser Console

1. Open http://localhost:3000
2. Press **F12** (Developer Tools)
3. Click **Console** tab
4. Look for red errors

**Common errors:**
- "Cannot find module" - Missing import
- "Failed to fetch" - Backend not running
- "React is not defined" - React not loaded
- CORS errors - Backend CORS issue

### 3. Check Network Tab

1. In Developer Tools, click **Network** tab
2. Refresh page (F5)
3. Check if files are loading:
   - `bundle.js` - Should load (200 status)
   - `main.chunk.js` - Should load
   - `static/css` - Should load

### 4. Quick Fixes to Try

#### Fix 1: Clear Cache and Restart
```powershell
cd "C:\Spring 2026\capstone_project\iot-monitoring-frontend"
# Stop npm start (Ctrl+C)
Remove-Item -Recurse -Force node_modules\.cache
npm start
```

#### Fix 2: Reinstall Dependencies
```powershell
cd "C:\Spring 2026\capstone_project\iot-monitoring-frontend"
Remove-Item -Recurse -Force node_modules
npm install --legacy-peer-deps
npm start
```

#### Fix 3: Check Backend
- Make sure backend is running: http://localhost:5286/swagger
- Frontend needs backend for API calls

#### Fix 4: Hard Refresh Browser
- Press **Ctrl + F5** or **Ctrl + Shift + R**
- Or clear browser cache

## What to Share

To help fix the issue, please share:

1. **Terminal output** - What do you see when `npm start` runs?
2. **Browser console errors** - F12 → Console tab → Any red errors?
3. **Network tab** - Are JavaScript files loading?
4. **Page source** - Right-click → View Source → Do you see HTML?

## Expected Behavior

When working correctly:
- Terminal shows "Compiled successfully!"
- Browser opens to http://localhost:3000
- You see "IoT Monitoring System" header
- Navigation bar appears
- Dashboard loads

## If Still Blank

Try this minimal test:

1. Stop the server (Ctrl+C)
2. Temporarily replace `src/App.tsx` with:
```tsx
import React from 'react';

function App() {
  return <div><h1>Test - React is working!</h1></div>;
}

export default App;
```

3. Run `npm start`
4. If this works, the issue is in the components
5. If this doesn't work, the issue is in the setup

Let me know what you see in the terminal and browser console!

