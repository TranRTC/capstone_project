# Troubleshooting Frontend - Blank Page

## Issue: http://localhost:3000 shows nothing

### Possible Causes & Solutions

#### 1. Server Not Running
**Check:**
- Look at the terminal where you ran `npm start`
- You should see compilation messages
- Look for "Compiled successfully!" message

**Solution:**
```powershell
cd "C:\Spring 2026\capstone_project\iot-monitoring-frontend"
npm start
```

#### 2. Compilation Errors
**Check:**
- Look for red error messages in the terminal
- Common errors:
  - Module not found
  - Syntax errors
  - Type errors

**Solution:**
- Fix the errors shown
- Or reinstall dependencies:
  ```powershell
  Remove-Item -Recurse -Force node_modules
  npm install --legacy-peer-deps
  ```

#### 3. Browser Cache
**Solution:**
- Hard refresh: `Ctrl + F5` or `Ctrl + Shift + R`
- Clear browser cache
- Try incognito/private mode

#### 4. Port Already in Use
**Check:**
```powershell
netstat -ano | findstr :3000
```

**Solution:**
- Kill the process using port 3000
- Or React will automatically use port 3001

#### 5. JavaScript Errors in Browser
**Check:**
- Open browser Developer Tools (F12)
- Go to Console tab
- Look for red error messages

**Common Errors:**
- "Cannot find module" - Missing dependency
- "Failed to fetch" - Backend not running
- CORS errors - Backend CORS not configured

#### 6. Missing Dependencies
**Solution:**
```powershell
cd iot-monitoring-frontend
npm install --legacy-peer-deps
```

## Quick Fix Steps

1. **Stop the server** (Ctrl+C in terminal)

2. **Clean and reinstall:**
   ```powershell
   cd "C:\Spring 2026\capstone_project\iot-monitoring-frontend"
   Remove-Item -Recurse -Force node_modules
   npm install --legacy-peer-deps
   ```

3. **Start again:**
   ```powershell
   npm start
   ```

4. **Wait for compilation:**
   - First time takes 30-60 seconds
   - Look for "Compiled successfully!"
   - Browser should open automatically

5. **If still blank:**
   - Check browser console (F12)
   - Check terminal for errors
   - Verify backend is running

## What You Should See

When working correctly:
- Terminal shows: "Compiled successfully!"
- Browser opens to http://localhost:3000
- You see "IoT Monitoring System" header
- Navigation bar appears
- Dashboard loads with statistics

## Check Terminal Output

The terminal should show:
```
Compiling...
Compiled successfully!

You can now view iot-monitoring-frontend in the browser.

  Local:            http://localhost:3000
  On Your Network:  http://192.168.x.x:3000
```

If you see errors instead, share them and I'll help fix them!

