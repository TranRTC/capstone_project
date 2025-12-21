# How to Check Frontend Status

## Step 1: Check Terminal Output

Look at the PowerShell window where `npm start` is running. You should see:

### ✅ Success Messages:
```
Compiling...
Compiled successfully!

You can now view iot-monitoring-frontend in the browser.

  Local:            http://localhost:3000
```

### ❌ Error Messages to Look For:
- "Cannot find module" - Missing dependency
- "Module not found" - Import error
- "Syntax error" - Code error
- "Failed to compile" - Build error

## Step 2: Check Browser Console

1. Open http://localhost:3000 in your browser
2. Press **F12** to open Developer Tools
3. Click the **Console** tab
4. Look for red error messages

### Common Browser Errors:
- "Failed to fetch" - Backend not running
- "Cannot read property" - Code error
- "Module not found" - Missing import
- CORS errors - Backend CORS issue

## Step 3: Check Network Tab

1. In Developer Tools, click **Network** tab
2. Refresh the page (F5)
3. Look for failed requests (red)
4. Check if files are loading (200 status = good)

## Step 4: Verify Files Exist

Make sure these files exist:
- `src/index.tsx` ✅
- `src/App.tsx` ✅
- `public/index.html` ✅
- `src/index.css` ✅

## Quick Diagnostic

Run this in PowerShell:
```powershell
cd "C:\Spring 2026\capstone_project\iot-monitoring-frontend"
npm run build
```

This will show all compilation errors.

## If Page is Completely Blank

1. **Check if HTML loads:**
   - Right-click page → View Page Source
   - You should see the HTML with `<div id="root"></div>`

2. **Check if JavaScript loads:**
   - Console should show no errors
   - Network tab should show .js files loading

3. **Check React:**
   - Console should not show "React is not defined"
   - Check if bundle.js is loading

## Share What You See

Please share:
1. What you see in the terminal (any errors?)
2. What you see in browser console (F12 → Console)
3. What you see when viewing page source (right-click → View Source)

This will help me fix the issue!

