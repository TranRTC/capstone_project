# Install Node.js - Step by Step Guide

## Quick Installation

### Option 1: Direct Download (Recommended)

1. **Go to Node.js website:**
   - Open your browser
   - Visit: https://nodejs.org/
   - You'll see two download buttons

2. **Download LTS Version:**
   - Click the **LTS** (Long Term Support) button
   - This is the recommended version (e.g., v20.x.x)
   - The file will be something like: `node-v20.x.x-x64.msi`

3. **Run the Installer:**
   - Double-click the downloaded `.msi` file
   - Follow the installation wizard:
     - Click "Next" on welcome screen
     - Accept the license agreement
     - Choose installation location (default is fine)
     - **IMPORTANT:** Make sure "Add to PATH" is checked
     - Click "Install"
     - Wait for installation to complete
     - Click "Finish"

4. **Verify Installation:**
   - Open a **NEW** PowerShell window (important - close and reopen)
   - Run these commands:
     ```powershell
     node --version
     npm --version
     ```
   - You should see version numbers (e.g., v20.11.0 and 10.2.4)

### Option 2: Using Chocolatey (If you have it)

```powershell
choco install nodejs-lts
```

### Option 3: Using Winget (Windows Package Manager)

```powershell
winget install OpenJS.NodeJS.LTS
```

---

## After Installation

### Step 1: Verify Node.js is Installed

Open PowerShell and run:
```powershell
node --version
npm --version
```

You should see:
```
v20.11.0    (or similar)
10.2.4      (or similar)
```

### Step 2: Navigate to Frontend Directory

```powershell
cd "C:\Spring 2026\capstone_project\iot-monitoring-frontend"
```

### Step 3: Install Dependencies

```powershell
npm install
```

This will take a few minutes. You'll see it downloading packages.

### Step 4: Start the Frontend

```powershell
npm start
```

The app will:
- Start the development server
- Open automatically in your browser at http://localhost:3000
- Show the React app

---

## Troubleshooting

### "node is not recognized"
- **Solution:** Close and reopen PowerShell
- If still not working, restart your computer
- Check if Node.js is in PATH: `$env:PATH`

### "npm install" fails
- Make sure you're in the `iot-monitoring-frontend` directory
- Check internet connection
- Try: `npm cache clean --force` then `npm install`

### Port 3000 already in use
```powershell
# Use a different port
$env:PORT=3001
npm start
```

### Cannot connect to API
- Make sure backend API is running on http://localhost:5286
- Check: http://localhost:5286/swagger should work
- Verify API URL in `src/services/api.ts`

---

## What to Expect

After `npm start`, you should see:
1. Browser opens automatically
2. React app loads at http://localhost:3000
3. You see the "IoT Monitoring System" header
4. Dashboard page with statistics
5. Navigation bar at the top

---

## Quick Test Checklist

- [ ] Node.js installed (`node --version` works)
- [ ] npm installed (`npm --version` works)
- [ ] Dependencies installed (`npm install` completed)
- [ ] Frontend starts (`npm start` works)
- [ ] Browser opens to http://localhost:3000
- [ ] Dashboard loads
- [ ] Backend API is running on http://localhost:5286

---

**Once Node.js is installed, let me know and I'll help you run the frontend!**

