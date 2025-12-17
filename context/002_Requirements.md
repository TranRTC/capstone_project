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

**Note:** For this capstone project, the focus is on core functionality demonstration. Advanced security features (authentication, authorization) and production-grade security measures are out of scope and can be added in future iterations.

### 1.3 Definitions and Acronyms
- **IoT:** Internet of Things
- **SCADA:** Supervisory Control and Data Acquisition
- **API:** Application Programming Interface
- **SPA:** Single Page Application
- **REST:** Representational State Transfer
- **MQTT:** Message Queuing Telemetry Transport

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
  - User can set monitoring parameters
  - Configuration changes are saved
  - Changes are reflected in real-time

#### FR-1.3: Device Status Monitoring
- **Description:** The system shall monitor and display the connection status of IoT devices (online/offline).
- **Priority:** High
- **Acceptance Criteria:**
  - System tracks device connection status
  - Status updates in real-time
  - Visual indicators show device status
  - Status history is logged

### 2.2 Data Collection

#### FR-2.1: Sensor Data Collection
- **Description:** The system shall collect sensor data from edge devices (microcontrollers and single-board computers) connected to sensors.
- **Priority:** High
- **Acceptance Criteria:**
  - Edge devices can send sensor data via HTTP/MQTT
  - System receives and validates sensor data
  - Data is stored in database
  - Data includes timestamp, device ID, sensor values

#### FR-2.2: Operational Metrics Collection
- **Description:** The system shall collect and calculate operational metrics (efficiency, throughput, utilization, etc.).
- **Priority:** Medium
- **Acceptance Criteria:**
  - System calculates metrics from sensor data
  - Metrics are stored in database
  - Metrics update in real-time
  - Historical metrics are available

#### FR-2.3: Status Information Collection
- **Description:** The system shall collect status information (online/offline, running/stopped, error codes).
- **Priority:** High
- **Acceptance Criteria:**
  - System receives status updates from devices
  - Status changes are logged
  - Status is displayed in real-time
  - Status history is maintained

### 2.3 Real-Time Monitoring

#### FR-3.1: Live Data Streaming
- **Description:** The system shall stream live sensor data updates to connected clients in real-time.
- **Priority:** High
- **Acceptance Criteria:**
  - New sensor data is pushed to clients via SignalR
  - Updates appear without page refresh
  - Multiple clients can receive updates simultaneously
  - Data latency is minimal (< 2 seconds)

#### FR-3.2: Equipment Status Display
- **Description:** The system shall display equipment status (working good, bad trend, or failure) in real-time.
- **Priority:** High
- **Acceptance Criteria:**
  - Status is visually indicated (colors/icons)
  - Status updates automatically
  - Status history is accessible
  - Status changes trigger alerts if configured

#### FR-3.3: Performance Metrics Tracking
- **Description:** The system shall track and display performance metrics in real-time.
- **Priority:** Medium
- **Acceptance Criteria:**
  - Metrics are calculated and updated
  - Metrics are displayed on dashboard
  - Metrics update in real-time
  - Historical trends are visible

### 2.4 Data Visualization

#### FR-4.1: Interactive Dashboards
- **Description:** The system shall provide interactive dashboards with customizable layouts.
- **Priority:** High
- **Acceptance Criteria:**
  - Users can view multiple data visualizations
  - Dashboard layout is customizable
  - Charts and graphs are interactive
  - Dashboard updates in real-time

#### FR-4.2: Historical Data Visualization
- **Description:** The system shall display historical data through charts and graphs for analysis.
- **Priority:** High
- **Acceptance Criteria:**
  - Users can select date ranges
  - Historical data is displayed in charts
  - Multiple data series can be compared
  - Data can be exported

#### FR-4.3: Data Charts and Graphs
- **Description:** The system shall provide various chart types (line, bar, gauge, etc.) for data visualization.
- **Priority:** Medium
- **Acceptance Criteria:**
  - Multiple chart types are available
  - Charts are responsive and interactive
  - Charts update in real-time for current data
  - Charts support historical data display

### 2.5 Alerting System

#### FR-5.1: Threshold-Based Alerts
- **Description:** The system shall generate alerts when sensor values exceed configured thresholds.
- **Priority:** High
- **Acceptance Criteria:**
  - Users can configure alert thresholds
  - System monitors values against thresholds
  - Alerts are triggered when thresholds are exceeded
  - Alerts are displayed to users

#### FR-5.2: Critical Event Notifications
- **Description:** The system shall notify users of critical events (equipment failure, offline status, etc.).
- **Priority:** High
- **Acceptance Criteria:**
  - Critical events are detected
  - Notifications appear immediately
  - Notifications are persistent until acknowledged
  - Notification history is maintained

#### FR-5.3: Configurable Alert Rules
- **Description:** The system shall allow users to configure custom alert rules and conditions.
- **Priority:** Medium
- **Acceptance Criteria:**
  - Users can create custom alert rules
  - Rules can combine multiple conditions
  - Rules can specify notification methods
  - Rules are saved and active

### 2.6 Historical Data Analysis

#### FR-6.1: Trace-Back Analysis
- **Description:** The system shall allow users to trace back and analyze historical status and sensor data.
- **Priority:** High
- **Acceptance Criteria:**
  - Users can query historical data by date range
  - Data can be filtered by device, sensor type
  - Historical status changes are visible
  - Data supports root cause analysis

#### FR-6.2: Data-Driven Decision Making
- **Description:** The system shall provide historical data and trends to support data-driven decision-making.
- **Priority:** Medium
- **Acceptance Criteria:**
  - Historical trends are calculated
  - Comparative analysis is available
  - Reports can be generated
  - Data can be exported for external analysis

### 2.7 User Interface

#### FR-7.1: Responsive Design
- **Description:** The system shall provide a responsive design for desktop (and mobile for future expansion).
- **Priority:** Medium
- **Acceptance Criteria:**
  - Interface adapts to desktop screen sizes
  - Layout is usable on different resolutions
  - Mobile support planned for future
  - Navigation is intuitive

#### FR-7.2: Intuitive Navigation
- **Description:** The system shall provide intuitive navigation between different sections and features.
- **Priority:** Medium
- **Acceptance Criteria:**
  - Navigation menu is clear and accessible
  - Users can easily find features
  - Breadcrumbs or navigation aids are provided
  - Consistent navigation patterns

#### FR-7.3: Role-Based Access Control (Out of Scope)
- **Description:** Role-based access control is planned for future implementation.
- **Priority:** Out of Scope for Capstone
- **Note:** This feature is not included in the initial capstone project scope but can be added in future iterations.

## 3. Non-Functional Requirements

### 3.1 Performance Requirements

#### NFR-1.1: Response Time
- **Description:** The system shall respond to user requests within acceptable time limits.
- **Priority:** High
- **Acceptance Criteria:**
  - API responses within 2 seconds for standard queries
  - Real-time updates delivered within 2 seconds
  - Dashboard loads within 3 seconds
  - Historical data queries complete within 5 seconds

#### NFR-1.2: Throughput
- **Description:** The system shall handle multiple concurrent users and devices.
- **Priority:** Medium
- **Acceptance Criteria:**
  - Support at least 10 concurrent users
  - Handle data from at least 50 IoT devices
  - Process at least 100 sensor readings per second
  - System remains responsive under load

#### NFR-1.3: Scalability
- **Description:** The system architecture shall support future expansion.
- **Priority:** Medium
- **Acceptance Criteria:**
  - Architecture supports adding more devices
  - Database can scale to handle more data
  - Code structure allows feature additions
  - System can be deployed on cloud infrastructure

### 3.2 Reliability Requirements

#### NFR-2.1: System Availability
- **Description:** The system shall be available for use during operational hours.
- **Priority:** Medium
- **Acceptance Criteria:**
  - System uptime target: 95% during development
  - Error handling prevents system crashes
  - Graceful degradation when components fail
  - Error messages are user-friendly

#### NFR-2.2: Data Integrity
- **Description:** The system shall maintain data integrity and prevent data loss.
- **Priority:** High
- **Acceptance Criteria:**
  - All sensor data is stored reliably
  - Database transactions are atomic
  - Data validation prevents invalid data storage
  - Backup and recovery procedures are documented

### 3.3 Security Requirements

**Note:** For capstone project scope, security is simplified to essential practices. Full authentication/authorization systems are out of scope.

#### NFR-3.1: Basic Input Validation
- **Description:** The system shall validate all user inputs and device data to prevent basic security issues.
- **Priority:** High
- **Acceptance Criteria:**
  - All inputs are validated (required fields, data types, ranges)
  - SQL injection prevention (using parameterized queries/Entity Framework)
  - Basic XSS prevention (React's built-in protection)
  - Invalid data is rejected with clear error messages

#### NFR-3.2: Data Transmission (Optional)
- **Description:** The system may use secure data transmission for production deployment.
- **Priority:** Low (Optional for capstone)
- **Acceptance Criteria:**
  - HTTPS can be configured for deployment
  - API endpoints are accessible (authentication optional for capstone)
  - Database connection is secured (connection string protection)

### 3.4 Usability Requirements

#### NFR-4.1: User Interface Design
- **Description:** The system shall provide an intuitive and user-friendly interface.
- **Priority:** Medium
- **Acceptance Criteria:**
  - Interface follows modern design principles
  - Error messages are clear and helpful
  - Loading indicators are provided
  - Interface is accessible and readable

#### NFR-4.2: Documentation
- **Description:** The system shall include user documentation and help resources.
- **Priority:** Low
- **Acceptance Criteria:**
  - User guide is available
  - API documentation is provided
  - Code comments explain complex logic
  - Setup instructions are clear

### 3.5 Compatibility Requirements

#### NFR-5.1: Browser Compatibility
- **Description:** The system shall work on modern web browsers.
- **Priority:** Medium
- **Acceptance Criteria:**
  - Compatible with Chrome, Edge, Firefox
  - WebSocket support required
  - Responsive design works across browsers
  - Graceful degradation for unsupported features

#### NFR-5.2: Device Compatibility
- **Description:** The system shall support communication with various edge devices.
- **Priority:** High
- **Acceptance Criteria:**
  - Supports HTTP REST API communication
  - Supports MQTT protocol (if implemented)
  - Compatible with Arduino, ESP modules, Raspberry Pi
  - Device registration is flexible

### 3.6 Maintainability Requirements

#### NFR-6.1: Code Quality
- **Description:** The codebase shall follow best practices and be maintainable.
- **Priority:** Medium
- **Acceptance Criteria:**
  - Code follows naming conventions
  - Code is well-commented
  - Architecture is modular
  - Code is version controlled (Git)

#### NFR-6.2: Documentation
- **Description:** Technical documentation shall be maintained.
- **Priority:** Medium
- **Acceptance Criteria:**
  - Architecture documentation exists
  - Database schema is documented
  - API endpoints are documented
  - Deployment procedures are documented

## 4. System Requirements

### 4.1 Hardware Requirements

#### SR-1.1: Server Requirements
- **Description:** Minimum hardware requirements for running the system.
- **Requirements:**
  - CPU: 2+ cores
  - RAM: 4GB minimum, 8GB recommended
  - Storage: 50GB minimum for database
  - Network: Stable internet connection

#### SR-1.2: Client Requirements
- **Description:** Minimum hardware requirements for client devices.
- **Requirements:**
  - Modern web browser (Chrome, Edge, Firefox)
  - Screen resolution: 1280x720 minimum
  - Internet connection for real-time updates

### 4.2 Software Requirements

#### SR-2.1: Server Software
- **Description:** Required software for server deployment.
- **Requirements:**
  - Windows Server or Linux
  - .NET 8.0 Runtime or later
  - SQL Server (Developer/Express/Standard Edition)
  - IIS or Kestrel web server

#### SR-2.2: Development Environment
- **Description:** Required software for development.
- **Requirements:**
  - Visual Studio 2022 or later
  - .NET 8.0 SDK
  - SQL Server Management Studio
  - Node.js and npm (for React development)
  - Git for version control

#### SR-2.3: Edge Device Software
- **Description:** Software requirements for edge devices.
- **Requirements:**
  - Microcontroller firmware (Arduino IDE, PlatformIO)
  - Network connectivity (WiFi/Ethernet)
  - HTTP client library or MQTT client library

## 5. Interface Requirements

### 5.1 User Interface
- Web-based interface accessible via browser
- Responsive design for desktop
- Real-time data updates via SignalR
- Interactive charts and dashboards

### 5.2 API Interface
- RESTful API for device management and data queries
- SignalR hub for real-time communication
- JSON data format
- HTTP/HTTPS protocol

### 5.3 Device Interface
- HTTP REST API for data transmission
- MQTT protocol support (optional for capstone)
- JSON payload format
- Basic device identification (authentication/authorization optional for capstone)

## 6. Constraints

### 6.1 Technical Constraints
- Must use C# for backend (ASP.NET Core)
- Must use React for frontend
- Must use SQL Server for database
- Must support real-time communication (SignalR)
- Must work with edge devices (Arduino, ESP, Raspberry Pi)

### 6.2 Project Constraints
- **Capstone project timeline:** Limited development time (typically one semester)
- **Academic requirements:** Must demonstrate core competencies and learning outcomes
- **Resource limitations:** Single developer or small team
- **Scope focus:** Core functionality over advanced features (security, scalability can be simplified)
- **Demonstration priority:** Working prototype that showcases technical skills

### 6.3 Business Constraints
- Cost-effective solution (free/open-source tools preferred)
- No commercial licensing costs for development
- Scalable to production if needed

## 7. Assumptions and Dependencies

### 7.1 Assumptions
- Edge devices have network connectivity (WiFi/Ethernet)
- Sensors are properly connected to edge devices
- Users have modern web browsers
- Development team has access to required hardware/software

### 7.2 Dependencies
- .NET 8.0 SDK and Runtime
- SQL Server availability
- Node.js and npm for React development
- Visual Studio or VS Code for development
- Internet connectivity for real-time features

## 8. Future Enhancements (Out of Scope)

The following features are planned for future expansion but are not included in the initial release:
- Mobile application (iOS/Android)
- Advanced analytics and machine learning
- Multi-tenant support
- Advanced role-based access control
- Cloud deployment and scaling
- Integration with external systems
- Advanced reporting and export features

## 9. Approval

- **Prepared by:** [Your Name]
- **Reviewed by:** [Reviewer Name]
- **Approved by:** [Approver Name]
- **Date:** [Date]

---

## Notes
This document is a living document and will be updated as requirements evolve during the project development lifecycle.

