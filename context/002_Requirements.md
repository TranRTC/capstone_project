# Requirements Document

## Document Information
- **Project:** Web-Based IoT Device Real-Time Monitoring System
- **Version:** 1.0
- **Date:** 2026
- **Status:** Draft

## 1. Introduction

### 1.1 Purpose
This document specifies the functional and non-functional requirements for the Web-Based IoT Device Real-Time Monitoring System. It defines what the system must do, how it should perform, and the constraints under which it must operate.

### 1.2 Scope
The system will provide real-time monitoring of IoT devices (equipment/device) in industrial, commercial, and agricultural facilities. It will collect sensor data, operational metrics, and status information from edge devices (microcontrollers and single-board computers) and provide web-based visualization, alerting, and historical data analysis capabilities.

**Note:** Advanced security features (complex authentication/authorization, role-based access control) are out of scope for the capstone project. Basic input validation and security measures will be implemented.

## 2. Functional Requirements

### 2.1 Device Management

#### FR-1.1: Device Registration
- **Description:** The system shall allow users to register new IoT devices (equipment/device).
- **Priority:** High
- **Acceptance Criteria:**
  - User can input device information (device ID, name, type, location)
  - System validates device information
  - Device is stored in database
  - Device appears in device list

#### FR-1.2: Device Configuration
- **Description:** The system shall allow users to configure device settings and parameters.
- **Priority:** High
- **Acceptance Criteria:**
  - User can update device metadata
  - Configuration changes are saved
  - Changes are reflected in the system

#### FR-1.3: Device Status Monitoring
- **Description:** The system shall monitor and display the connection status of IoT devices (online/offline).
- **Priority:** High
- **Acceptance Criteria:**
  - System tracks device connection status
  - Status updates in real-time
  - Visual indicators show device status

### 2.2 Data Collection

#### FR-2.1: Sensor Data Collection
- **Description:** The system shall collect sensor data from edge devices (microcontrollers and single-board computers) connected to sensors.
- **Priority:** High
- **Acceptance Criteria:**
  - Edge devices can send sensor data via HTTP/MQTT
  - Data is received and validated
  - Data includes timestamp, device ID, sensor values
  - Data is stored in database

#### FR-2.2: Operational Metrics Collection
- **Description:** The system shall collect operational metrics from devices.
- **Priority:** Medium
- **Acceptance Criteria:**
  - System receives status updates from devices
  - Metrics are calculated and stored
  - Metrics are available for analysis

### 2.3 Real-Time Monitoring

#### FR-3.1: Real-Time Data Display
- **Description:** The system shall display real-time sensor data and device status.
- **Priority:** High
- **Acceptance Criteria:**
  - Data updates automatically without page refresh
  - Updates occur within 1-2 seconds of data receipt
  - Multiple devices can be monitored simultaneously

#### FR-3.2: Equipment Status Display
- **Description:** The system shall display equipment status (working good, bad trend, or failure) in real-time.
- **Priority:** High
- **Acceptance Criteria:**
  - Status is clearly visible on dashboard
  - Status updates in real-time
  - Historical status changes are tracked

### 2.4 Data Visualization

#### FR-4.1: Dashboard Display
- **Description:** The system shall provide a dashboard with visual representations of data.
- **Priority:** High
- **Acceptance Criteria:**
  - Dashboard displays key metrics
  - Charts and graphs are interactive
  - Dashboard is responsive and usable

#### FR-4.2: Historical Data Visualization
- **Description:** The system shall display historical data in charts and graphs.
- **Priority:** High
- **Acceptance Criteria:**
  - Users can view historical trends
  - Data can be filtered by date range
  - Multiple visualization types are available

### 2.5 Alerting System

#### FR-5.1: Threshold-Based Alerts
- **Description:** The system shall generate alerts when sensor values exceed configured thresholds.
- **Priority:** High
- **Acceptance Criteria:**
  - Users can configure alert thresholds
  - Alerts are triggered when thresholds are exceeded
  - Alerts are displayed to users

#### FR-5.2: Alert Notifications
- **Description:** The system shall notify users of critical events (equipment failure, offline status, etc.).
- **Priority:** High
- **Acceptance Criteria:**
  - Notifications appear in real-time
  - Alerts are visible on dashboard
  - Alert history is maintained

#### FR-5.3: Alert Management
- **Description:** The system shall allow users to acknowledge and resolve alerts.
- **Priority:** Medium
- **Acceptance Criteria:**
  - Users can acknowledge alerts
  - Users can mark alerts as resolved
  - Alert status is tracked

### 2.6 Historical Data Analysis

#### FR-6.1: Data Query
- **Description:** The system shall allow users to query historical sensor data.
- **Priority:** High
- **Acceptance Criteria:**
  - Users can filter data by device, sensor type
  - Users can filter by date range
  - Query results are displayed clearly

#### FR-6.2: Data Export
- **Description:** The system shall allow users to export historical data.
- **Priority:** Low
- **Acceptance Criteria:**
  - Data can be exported in common formats (CSV, JSON)
  - Export includes selected date range and filters

### 2.7 User Management

#### FR-7.1: Basic User Interface
- **Description:** The system shall provide a web-based user interface accessible via a web browser.
- **Priority:** High
- **Acceptance Criteria:**
  - Interface is responsive and works on desktop
  - Navigation is intuitive
  - Interface is accessible via standard web browsers

#### FR-7.2: User Authentication (Basic)
- **Description:** The system shall provide basic user authentication.
- **Priority:** Medium
- **Acceptance Criteria:**
  - Users can log in with credentials
  - Session management is functional
  - Basic security measures are in place

#### FR-7.3: Role-Based Access Control
- **Description:** The system shall support role-based access control.
- **Priority:** Low
- **Status:** **Out of Scope for Capstone**
- **Note:** Advanced authorization features are deferred to future enhancements.

## 3. Non-Functional Requirements

### 3.1 Performance Requirements

#### NFR-1.1: Response Time
- **Description:** The system shall respond to user requests within acceptable time limits.
- **Priority:** High
- **Acceptance Criteria:**
  - API responses within 500ms for standard queries
  - Real-time updates within 1-2 seconds
  - Dashboard loads within 3 seconds

#### NFR-1.2: Scalability
- **Description:** The system shall handle multiple concurrent users and devices.
- **Priority:** Medium
- **Acceptance Criteria:**
  - Support at least 10 concurrent users
  - Handle data from at least 50 IoT devices
  - System remains responsive under load

#### NFR-1.3: Data Throughput
- **Description:** The system shall handle high-volume data ingestion.
- **Priority:** Medium
- **Acceptance Criteria:**
  - Process at least 100 sensor readings per second
  - Architecture supports adding more devices

### 3.2 Reliability Requirements

#### NFR-2.1: System Availability
- **Description:** The system shall be available for use during normal operating hours.
- **Priority:** Medium
- **Acceptance Criteria:**
  - System uptime target: 95% during development/testing
  - Error handling for common failure scenarios

#### NFR-2.2: Data Integrity
- **Description:** The system shall maintain data integrity and prevent data loss.
- **Priority:** High
- **Acceptance Criteria:**
  - Data is persisted reliably
  - Database transactions ensure consistency
  - Backup strategy is implemented

### 3.3 Security Requirements

#### SR-1.1: Input Validation
- **Description:** The system shall validate all user inputs and device data to prevent basic security issues.
- **Priority:** High
- **Acceptance Criteria:**
  - SQL injection prevention
  - XSS (Cross-Site Scripting) prevention
  - Input sanitization and validation

#### SR-1.2: Data Protection
- **Description:** The system shall protect sensitive data.
- **Priority:** Medium
- **Acceptance Criteria:**
  - Sensitive data is not exposed in logs
  - Basic encryption for data in transit (HTTPS)

#### SR-1.3: Authentication and Authorization
- **Description:** The system shall provide authentication and authorization mechanisms.
- **Priority:** Medium
- **Status:** **Simplified for Capstone**
- **Note:** Basic authentication will be implemented. Advanced features (OAuth, JWT tokens, role-based permissions) are out of scope for capstone.

### 3.4 Usability Requirements

#### NFR-3.1: User Interface Design
- **Description:** The system shall provide an intuitive and user-friendly interface.
- **Priority:** High
- **Acceptance Criteria:**
  - Interface is easy to navigate
  - Information is clearly presented
  - Responsive design for desktop

#### NFR-3.2: Documentation
- **Description:** The system shall include user documentation.
- **Priority:** Medium
- **Acceptance Criteria:**
  - User guide is available
  - API documentation is provided

### 3.5 Compatibility Requirements

#### NFR-4.1: Browser Compatibility
- **Description:** The system shall work with modern web browsers.
- **Priority:** High
- **Acceptance Criteria:**
  - Compatible with Chrome, Edge, Firefox (latest versions)
  - Responsive design works on desktop

#### NFR-5.1: Edge Device Compatibility
- **Description:** The system shall support communication with various edge devices.
- **Priority:** High
- **Acceptance Criteria:**
  - Supports Arduino, ESP modules, Raspberry Pi
  - Supports HTTP and MQTT protocols
  - Device registration is flexible

## 4. System Requirements

### 4.1 Hardware Requirements

#### SR-2.1: Server Hardware
- **Description:** Minimum hardware requirements for the server.
- **Acceptance Criteria:**
  - CPU: 2+ cores
  - RAM: 4GB minimum, 8GB recommended
  - Storage: 50GB minimum for database

#### SR-2.2: Client Hardware
- **Description:** Minimum hardware requirements for client devices.
- **Acceptance Criteria:**
  - Modern desktop computer
  - Internet connection
  - Modern web browser

#### SR-2.3: Edge Device Software
- **Description:** Software requirements for edge devices.
- **Acceptance Criteria:**
  - Edge devices have network connectivity (WiFi/Ethernet)
  - Sensors are properly connected to edge devices
  - Edge devices can send HTTP/MQTT messages

### 4.2 Software Requirements

#### SR-3.1: Server Software
- **Description:** Software requirements for the server.
- **Acceptance Criteria:**
  - Windows Server or Windows 10/11 (for development)
  - .NET 8.0 Runtime
  - SQL Server (Express edition acceptable for capstone)
  - IIS or Kestrel web server

#### SR-3.2: Development Tools
- **Description:** Development tools required.
- **Acceptance Criteria:**
  - Visual Studio 2022 or Visual Studio Code
  - Node.js and npm for React development
  - Git for version control

## 5. Interface Requirements

### 5.1 User Interface
- Web-based interface accessible via a web browser
- Responsive design for desktop
- Real-time data updates via SignalR
- Interactive charts and dashboards

### 5.2 API Interface
- RESTful API for device management and data queries
- SignalR hub for real-time communication
- Standard HTTP/HTTPS protocols
- JSON data format

### 5.3 Device Interface
- HTTP POST endpoints for data ingestion
- MQTT support (optional)
- Basic device identification (authentication/authorization optional for capstone)
- JSON payload format

### 5.4 Database Interface
- SQL Server database
- Entity Framework Core for data access
- Standard SQL queries and transactions

## 6. Constraints

### 6.1 Technical Constraints
- Must use C# backend (ASP.NET Core)
- Must use React frontend
- Must use SQL Server database
- Must support real-time communication (SignalR)
- Must work with edge devices (Arduino, ESP, Raspberry Pi)

### 6.2 Project Constraints
- **Capstone Project Limitations:**
  - Limited time and resources
  - Focus on core functionality
  - Advanced security features are out of scope
  - Role-based access control is deferred
  - Mobile support is future enhancement
  - Complex analytics are future enhancement

### 6.3 Business Constraints
- Must be cost-effective (use free/open-source tools where possible)
- Must be deployable on standard hardware
- Must be maintainable by developers with standard skills

## 7. Assumptions
- Edge devices have network connectivity (WiFi/Ethernet)
- Sensors are properly connected to edge devices
- Users have basic computer and web browsing skills
- System will be used in controlled environments (not public internet initially)

## 8. Out of Scope (For Capstone)
The following features are explicitly out of scope for the capstone project but may be considered for future enhancements:
- Advanced authentication/authorization (OAuth, JWT, multi-factor authentication)
- Role-based access control with complex permissions
- Mobile application
- Advanced analytics and machine learning
- Multi-tenant architecture
- Advanced reporting and business intelligence
- Integration with external systems (ERP, MES)
- Complex workflow automation

## 9. Future Enhancements
- Mobile application (iOS/Android)
- Advanced analytics and predictive maintenance
- Machine learning for anomaly detection
- Enhanced security features
- Multi-tenant support
- Advanced reporting
- Integration capabilities

