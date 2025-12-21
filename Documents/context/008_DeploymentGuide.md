# Deployment Guide

## Document Information
- **Project:** Web-Based IoT Device Real-Time Monitoring System
- **Version:** 1.0
- **Date:** 2026
- **Status:** Draft

## 1. Introduction

### 1.1 Purpose
This document provides step-by-step instructions for setting up, configuring, and deploying the Web-Based IoT Device Real-Time Monitoring System. It covers development environment setup, database configuration, backend deployment, frontend deployment, and production deployment considerations.

### 1.2 Scope
This guide covers:
- Development environment setup
- Database installation and configuration
- Backend API deployment
- Frontend application deployment
- Configuration and environment variables
- Troubleshooting common issues

**Note:** For capstone project, deployment focuses on local development and basic production deployment. Advanced production deployment (Docker, cloud services) may be optional.

### 1.3 Prerequisites
- Windows 10/11 (or Windows Server)
- Visual Studio 2022 or Visual Studio Code
- .NET 8.0 SDK
- Node.js 18+ and npm
- SQL Server (Express, Developer, or Standard Edition)
- Basic knowledge of C#, React, and SQL Server

## 2. System Requirements

### 2.1 Development Environment

#### Minimum Requirements
- **OS:** Windows 10/11 (64-bit)
- **CPU:** Dual-core processor, 2.0 GHz or higher
- **RAM:** 8 GB (16 GB recommended)
- **Storage:** 20 GB free space
- **Network:** Internet connection for package downloads

#### Recommended Requirements
- **OS:** Windows 11 (64-bit)
- **CPU:** Quad-core processor, 3.0 GHz or higher
- **RAM:** 16 GB
- **Storage:** 50 GB free space (SSD recommended)
- **Network:** Stable internet connection

### 2.2 Production Environment

#### Minimum Requirements
- **OS:** Windows Server 2019/2022 or Windows 10/11
- **CPU:** Quad-core processor, 2.5 GHz or higher
- **RAM:** 16 GB
- **Storage:** 100 GB free space (SSD recommended)
- **Network:** Stable internet connection, static IP (if needed)

## 3. Development Environment Setup

### 3.1 Install Required Software

#### Step 1: Install .NET 8.0 SDK
1. Download .NET 8.0 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0
2. Run the installer
3. Verify installation:
   ```powershell
   dotnet --version
   ```
   Expected output: `8.0.x`

#### Step 2: Install Visual Studio 2022
1. Download Visual Studio 2022 Community (free) from: https://visualstudio.microsoft.com/
2. During installation, select:
   - **ASP.NET and web development** workload
   - **.NET desktop development** workload (optional)
3. Complete installation and restart if required

#### Step 3: Install Node.js and npm
1. Download Node.js LTS version from: https://nodejs.org/
2. Run the installer (includes npm)
3. Verify installation:
   ```powershell
   node --version
   npm --version
   ```
   Expected: Node.js 18.x or higher, npm 9.x or higher

#### Step 4: Install SQL Server
1. Download SQL Server Express (free) or Developer Edition (free) from: https://www.microsoft.com/sql-server/sql-server-downloads
2. Run the installer
3. Select "Basic" installation type (for development)
4. Note the server name (usually `localhost` or `.\SQLEXPRESS`)
5. Verify installation:
   - Open SQL Server Management Studio (SSMS) or Azure Data Studio
   - Connect to the server

**Alternative:** Use SQL Server LocalDB (included with Visual Studio)

### 3.2 Install Additional Tools (Optional)

- **Postman:** For API testing (https://www.postman.com/)
- **Git:** For version control (https://git-scm.com/)
- **Azure Data Studio:** For database management (https://azure.microsoft.com/products/data-studio/)

## 4. Database Setup

### 4.1 Create Database

#### Option 1: Using SQL Server Management Studio (SSMS)
1. Open SSMS
2. Connect to your SQL Server instance
3. Right-click "Databases" → "New Database"
4. Database name: `IoTMonitoringDB`
5. Click "OK"

#### Option 2: Using SQL Script
```sql
CREATE DATABASE IoTMonitoringDB;
GO

USE IoTMonitoringDB;
GO
```

#### Option 3: Using Entity Framework Core Migrations
The database will be created automatically when you run migrations (see Backend Setup section).

### 4.2 Configure Database Connection

The connection string will be configured in `appsettings.json` (see Backend Configuration section).

### 4.3 Verify Database Access
1. Open SSMS or Azure Data Studio
2. Connect to `IoTMonitoringDB`
3. Verify you can query the database:
   ```sql
   SELECT @@VERSION;
   ```

## 5. Backend Setup and Deployment

### 5.1 Create Backend Project

#### Option 1: Using Visual Studio
1. Open Visual Studio 2022
2. File → New → Project
3. Select "ASP.NET Core Web API"
4. Project name: `IoTMonitoringSystem.API`
5. Framework: .NET 8.0
6. Authentication: None (for capstone)
7. Click "Create"

#### Option 2: Using .NET CLI
```powershell
# Create solution
dotnet new sln -n IoTMonitoringSystem

# Create API project
dotnet new webapi -n IoTMonitoringSystem.API -o IoTMonitoringSystem.API

# Add project to solution
dotnet sln add IoTMonitoringSystem.API/IoTMonitoringSystem.API.csproj
```

### 5.2 Install Required NuGet Packages

```powershell
cd IoTMonitoringSystem.API

# Entity Framework Core
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.EntityFrameworkCore.Design

# SignalR
dotnet add package Microsoft.AspNetCore.SignalR

# Other packages (as needed)
dotnet add package Swashbuckle.AspNetCore
dotnet add package FluentValidation.AspNetCore
```

### 5.3 Configure Database Connection

Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=IoTMonitoringDB;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**For SQL Server Express:**
```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=IoTMonitoringDB;Trusted_Connection=True;MultipleActiveResultSets=true"
```

### 5.4 Set Up Entity Framework Core

1. Create `ApplicationDbContext` (see Application Design Document)
2. Configure DbContext in `Program.cs`:
   ```csharp
   builder.Services.AddDbContext<ApplicationDbContext>(options =>
       options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
   ```

3. Create initial migration:
   ```powershell
   dotnet ef migrations add InitialCreate --project IoTMonitoringSystem.API
   ```

4. Apply migration to create database:
   ```powershell
   dotnet ef database update --project IoTMonitoringSystem.API
   ```

### 5.5 Configure SignalR

In `Program.cs`:
```csharp
// Add SignalR
builder.Services.AddSignalR();

// Configure CORS (for frontend)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Map SignalR hub
app.MapHub<MonitoringHub>("/hubs/monitoring");
```

### 5.6 Run Backend Application

#### Development Mode
```powershell
cd IoTMonitoringSystem.API
dotnet run
```

Or press F5 in Visual Studio.

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

### 5.7 Verify Backend is Running

1. Open browser and navigate to: `https://localhost:5001/swagger`
2. You should see the Swagger UI with API endpoints
3. Test an endpoint (e.g., GET /devices)

## 6. Frontend Setup and Deployment

### 6.1 Create Frontend Project

#### Option 1: Using Create React App
```powershell
npx create-react-app iot-monitoring-frontend --template typescript
cd iot-monitoring-frontend
```

#### Option 2: Using Vite (Recommended - Faster)
```powershell
npm create vite@latest iot-monitoring-frontend -- --template react-ts
cd iot-monitoring-frontend
npm install
```

### 6.2 Install Required Packages

```powershell
# Routing
npm install react-router-dom

# HTTP Client
npm install axios

# SignalR Client
npm install @microsoft/signalr

# Charts
npm install chart.js react-chartjs-2
# OR
npm install recharts

# UI Framework (choose one)
npm install @mui/material @emotion/react @emotion/styled
# OR
npm install antd
# OR
npm install -D tailwindcss

# TypeScript types
npm install -D @types/react-router-dom
```

### 6.3 Configure API Base URL

Create `.env` file in frontend root:

```env
REACT_APP_API_BASE_URL=http://localhost:5001/api/v1
REACT_APP_SIGNALR_URL=http://localhost:5001/hubs/monitoring
```

**For Vite:**
```env
VITE_API_BASE_URL=http://localhost:5001/api/v1
VITE_SIGNALR_URL=http://localhost:5001/hubs/monitoring
```

### 6.4 Configure API Client

Create `src/services/api/apiClient.ts`:

```typescript
import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5001/api/v1';

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json'
  }
});
```

### 6.5 Run Frontend Application

#### Development Mode
```powershell
cd iot-monitoring-frontend
npm start
# OR for Vite
npm run dev
```

The frontend will be available at:
- `http://localhost:3000` (Create React App)
- `http://localhost:5173` (Vite)

### 6.6 Verify Frontend is Running

1. Open browser and navigate to: `http://localhost:3000` (or `http://localhost:5173`)
2. You should see the application home page
3. Verify API connection by checking browser console for errors

## 7. Configuration

### 7.1 Backend Configuration (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=IoTMonitoringDB;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "AllowedHosts": "*",
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:5173"
    ]
  }
}
```

### 7.2 Frontend Configuration (`.env`)

```env
# API Configuration
VITE_API_BASE_URL=http://localhost:5001/api/v1
VITE_SIGNALR_URL=http://localhost:5001/hubs/monitoring

# Environment
VITE_ENV=development
```

### 7.3 Production Configuration

#### Backend (`appsettings.Production.json`)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=IoTMonitoringDB;User Id=YOUR_USER;Password=YOUR_PASSWORD;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Cors": {
    "AllowedOrigins": [
      "https://yourdomain.com"
    ]
  }
}
```

#### Frontend (Production `.env`)
```env
VITE_API_BASE_URL=https://api.yourdomain.com/api/v1
VITE_SIGNALR_URL=https://api.yourdomain.com/hubs/monitoring
VITE_ENV=production
```

## 8. Production Deployment

### 8.1 Backend Deployment

#### Option 1: IIS Deployment
1. Publish the backend:
   ```powershell
   dotnet publish -c Release -o ./publish
   ```

2. Copy published files to IIS directory (e.g., `C:\inetpub\wwwroot\IoTMonitoringAPI`)

3. Create IIS Application Pool:
   - Open IIS Manager
   - Create new Application Pool
   - Set .NET CLR Version to "No Managed Code"
   - Set Managed Pipeline Mode to "Integrated"

4. Create IIS Website:
   - Right-click "Sites" → "Add Website"
   - Set physical path to published folder
   - Set binding (HTTP/HTTPS)
   - Select the Application Pool created above

5. Install ASP.NET Core Hosting Bundle:
   - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
   - Install on server

#### Option 2: Self-Hosted (Console Application)
1. Publish the backend:
   ```powershell
   dotnet publish -c Release -o ./publish
   ```

2. Run as Windows Service:
   - Use NSSM (Non-Sucking Service Manager) or similar
   - Configure to start automatically

### 8.2 Frontend Deployment

#### Option 1: Static File Hosting
1. Build the frontend:
   ```powershell
   npm run build
   ```

2. Deploy `build` folder (or `dist` for Vite) to:
   - IIS static file hosting
   - Nginx
   - Apache
   - Cloud storage (Azure Blob, AWS S3)

#### Option 2: IIS Deployment
1. Build the frontend:
   ```powershell
   npm run build
   ```

2. Copy `build` folder contents to IIS directory

3. Configure IIS:
   - Create new website
   - Point to build folder
   - Configure URL rewrite (for React Router)

### 8.3 Database Deployment

1. Backup development database:
   ```sql
   BACKUP DATABASE IoTMonitoringDB
   TO DISK = 'C:\Backups\IoTMonitoringDB.bak';
   ```

2. Restore to production server:
   ```sql
   RESTORE DATABASE IoTMonitoringDB
   FROM DISK = 'C:\Backups\IoTMonitoringDB.bak';
   ```

3. Or use Entity Framework migrations:
   ```powershell
   dotnet ef database update --project IoTMonitoringSystem.API
   ```

## 9. Troubleshooting

### 9.1 Common Backend Issues

#### Issue: Database Connection Failed
**Symptoms:** Error: "Cannot open database"
**Solutions:**
- Verify SQL Server is running
- Check connection string in `appsettings.json`
- Verify database exists
- Check SQL Server authentication (Windows Authentication vs SQL Authentication)

#### Issue: CORS Errors
**Symptoms:** Browser console shows CORS errors
**Solutions:**
- Verify CORS is configured in `Program.cs`
- Check `AllowedOrigins` includes frontend URL
- Ensure `AllowCredentials()` is called if using cookies

#### Issue: SignalR Connection Failed
**Symptoms:** SignalR cannot connect
**Solutions:**
- Verify SignalR hub is mapped in `Program.cs`
- Check CORS configuration allows SignalR
- Verify WebSocket is enabled (IIS: Enable WebSocket Protocol)

### 9.2 Common Frontend Issues

#### Issue: API Calls Fail
**Symptoms:** Network errors in browser console
**Solutions:**
- Verify backend is running
- Check API base URL in `.env` file
- Verify CORS is configured on backend
- Check browser console for detailed error messages

#### Issue: SignalR Cannot Connect
**Symptoms:** SignalR connection fails
**Solutions:**
- Verify SignalR URL is correct
- Check backend SignalR hub is running
- Verify WebSocket support in browser
- Check network/firewall settings

#### Issue: Build Errors
**Symptoms:** `npm run build` fails
**Solutions:**
- Delete `node_modules` and `package-lock.json`
- Run `npm install` again
- Check Node.js version (should be 18+)
- Verify all dependencies are installed

### 9.3 Database Issues

#### Issue: Migration Fails
**Symptoms:** `dotnet ef database update` fails
**Solutions:**
- Verify connection string is correct
- Check database server is accessible
- Ensure user has permissions to create database
- Try creating database manually first

#### Issue: Tables Not Created
**Symptoms:** Database exists but no tables
**Solutions:**
- Run migrations: `dotnet ef database update`
- Check migration files exist in `Migrations` folder
- Verify `ApplicationDbContext` is configured correctly

## 10. Security Considerations

### 10.1 Development Environment
- Use Windows Authentication for SQL Server (local development)
- Keep connection strings in `appsettings.json` (not in source control)
- Use HTTPS in development (localhost certificates)

### 10.2 Production Environment
- Use SQL Authentication with strong passwords
- Store connection strings in secure configuration (Azure Key Vault, environment variables)
- Enable HTTPS only
- Configure firewall rules
- Implement authentication/authorization (if required)
- Regular security updates

## 11. Maintenance

### 11.1 Regular Tasks
- **Database Backups:** Schedule regular database backups
- **Log Monitoring:** Monitor application logs for errors
- **Updates:** Keep .NET SDK, Node.js, and dependencies updated
- **Security Patches:** Apply security updates regularly

### 11.2 Monitoring
- Monitor application performance
- Monitor database size and performance
- Monitor SignalR connections
- Set up alerts for critical errors

## 12. Notes

- For capstone project, local development deployment is sufficient
- Production deployment can be simplified (single server deployment)
- Docker containerization is optional for capstone
- Cloud deployment (Azure, AWS) is optional but recommended for portfolio

---

## Approval

- **Prepared by:** [Your Name]
- **Reviewed by:** [Reviewer Name]
- **Approved by:** [Approver Name]
- **Date:** [Date]

---

## Notes

This document is a living document and will be updated as deployment processes evolve.

