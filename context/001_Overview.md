# Project Overview

## Project Name
Web_Based Industrial Equipment Real-Time Monitoring System

## Purpose
This capstone project is a full-stack web application designed for real-time monitoring of industrial equipment and devices. The system provides live data visualization, alerts, and analytics to help operators and managers track equipment performance, detect anomalies, and maintain optimal operational conditions.

## Project Description
The Industrial Equipment Real-Time Monitoring System is a comprehensive solution that:
- **Monitors** industrial equipment and devices in real-time
- **Collects** sensor data, operational metrics, and status information
- **Visualizes** data through interactive dashboards and charts
- **Alerts** users to critical events, anomalies, or threshold breaches
- **Tracks** historical performance and trends
- **Supports** multiple devices and equipment types simultaneously

## Technical Stack
- **Backend:** C# (ASP.NET Core Web API)
- **Frontend:** React (with modern UI framework)
- **Real-Time Communication:** SignalR (for WebSocket-based real-time updates)
- **Database:** (To be determined - SQL Server/PostgreSQL recommended)
- **Data Visualization:** Chart.js, D3.js, or similar library

## Key Features (Planned)
1. **Real-Time Data Streaming**
   - Live sensor data updates
   - Equipment status monitoring
   - Performance metrics tracking

2. **Dashboard & Visualization**
   - Interactive charts and graphs
   - Customizable dashboard layouts
   - Historical data analysis

3. **Alerting System**
   - Threshold-based alerts
   - Critical event notifications
   - Configurable alert rules

4. **Device Management**
   - Device registration and configuration
   - Equipment metadata management
   - Connection status monitoring

5. **User Interface**
   - Responsive design for desktop and mobile
   - Intuitive navigation
   - Role-based access control (if applicable)

## Architecture Overview
- **Backend API:** RESTful API built with ASP.NET Core for data management and business logic
- **Real-Time Hub:** SignalR hub for bidirectional communication between server and clients
- **Frontend Application:** React SPA with component-based architecture
- **Data Layer:** Database for persistent storage of historical data and configurations

## Current Status
- Project repository initialized
- GitHub connected
- 3-2-1 backup strategy set up
- Google Drive used for ZIP snapshot backups (no auto-sync)

## Key Tools
- Git + GitHub for version control
- Google Drive for disaster backups (ZIP files only)
- Windows development environment

## Important Rules
- Do NOT work inside Google Drive folders
- Commit and push to GitHub regularly
- Take ZIP snapshot backups weekly or before risky changes

## Development Phases (Planned)
1. **Phase 1:** Project setup and architecture design
2. **Phase 2:** Backend API development (C#)
3. **Phase 3:** Real-time communication implementation (SignalR)
4. **Phase 4:** Frontend development (React)
5. **Phase 5:** Data visualization and dashboard
6. **Phase 6:** Alerting and notification system
7. **Phase 7:** Testing and optimization
8. **Phase 8:** Documentation and deployment

## Notes
This file is the main context reference.  
When starting a new chat or returning after a break, read this first.

## Cursor Desktop Mode Recommendation
For this capstone project, the following Cursor modes are recommended:
- **Composer Mode:** Best for complex, multi-file changes (e.g., implementing features across backend and frontend, refactoring, adding new components)
- **Chat Mode:** Ideal for quick questions, debugging, code explanations, and single-file edits
- **Inline Edit:** Useful for small, focused changes within a single file

**Most Suitable:** Start with **Composer Mode** for feature development and architecture work, as this project involves coordinating changes across multiple files and layers (API, SignalR, React components, etc.).
