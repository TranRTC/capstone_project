# React Frontend Setup Guide

## Prerequisites

### Step 1: Install Node.js

You need Node.js to run React applications. Here's how to install it:

1. **Download Node.js:**
   - Go to: https://nodejs.org/
   - Download the **LTS version** (Long Term Support)
   - Choose the Windows Installer (.msi)

2. **Install Node.js:**
   - Run the installer
   - Follow the installation wizard
   - Make sure to check "Add to PATH" option
   - Complete the installation

3. **Verify Installation:**
   Open PowerShell and run:
   ```powershell
   node --version
   npm --version
   ```
   You should see version numbers (e.g., v20.x.x and 10.x.x)

### Step 2: Install Node.js (Alternative - Using Chocolatey)

If you have Chocolatey package manager:
```powershell
choco install nodejs-lts
```

---

## After Installing Node.js

Once Node.js is installed, come back and I'll help you:
1. Create the React application
2. Set up all dependencies
3. Create the project structure
4. Connect to your API
5. Implement SignalR client

---

## Quick Setup Commands (After Node.js is Installed)

```powershell
# Navigate to project directory
cd "C:\Spring 2026\capstone_project"

# Create React app with TypeScript
npx create-react-app@latest iot-monitoring-frontend --template typescript

# Navigate to frontend directory
cd iot-monitoring-frontend

# Install additional dependencies
npm install @microsoft/signalr axios react-router-dom recharts
npm install @mui/material @emotion/react @emotion/styled
npm install @mui/icons-material

# Start development server
npm start
```

---

## What We'll Build

Once Node.js is installed, I'll help you create:

1. **React Application Structure**
   - TypeScript setup
   - Routing configuration
   - Component hierarchy

2. **API Integration**
   - API service layer
   - Axios configuration
   - Error handling

3. **SignalR Client**
   - Real-time connection
   - Event handlers
   - Reconnection logic

4. **Pages & Components**
   - Dashboard with real-time charts
   - Device management
   - Sensor management
   - Alert management
   - Data visualization

5. **UI Components**
   - Material-UI components
   - Charts and graphs
   - Forms and tables
   - Navigation

---

## Next Steps

1. **Install Node.js** (if not already installed)
2. **Verify installation** with `node --version`
3. **Let me know** when Node.js is installed
4. **I'll create the React app** and set everything up!

---

## Manual Setup (If You Prefer)

If you want to set up manually or Node.js installation is delayed, I can:
- Create the project structure manually
- Create all the files and folders
- Set up package.json with dependencies
- You can install dependencies later when Node.js is ready

Would you like me to create the structure manually now, or wait until Node.js is installed?

