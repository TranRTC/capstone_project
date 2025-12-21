# User Manual

## Document Information
- **Project:** Web-Based IoT Device Real-Time Monitoring System
- **Version:** 1.0
- **Date:** 2026
- **Status:** Draft

## 1. Introduction

### 1.1 Purpose
This user manual provides instructions for using the Web-Based IoT Device Real-Time Monitoring System. It guides users through all features and functionalities of the system.

### 1.2 Audience
This manual is intended for:
- System administrators
- Operators monitoring IoT devices
- Maintenance personnel
- End users of the system

### 1.3 System Overview
The IoT Device Real-Time Monitoring System allows you to:
- Monitor IoT devices and sensors in real-time
- View historical data and trends
- Configure alert rules
- Manage alerts and notifications
- Track device status and health

## 2. Getting Started

### 2.1 Accessing the System

1. Open your web browser (Chrome, Firefox, Edge, or Safari)
2. Navigate to the system URL (provided by your administrator)
3. The system login page will appear (if authentication is enabled)

**Note:** For capstone project, authentication may be simplified or disabled.

### 2.2 System Requirements

- **Browser:** Modern web browser (Chrome, Firefox, Edge, Safari)
- **Screen Resolution:** Minimum 1280x720 (1920x1080 recommended)
- **Internet Connection:** Stable connection for real-time updates
- **JavaScript:** Must be enabled

### 2.3 First Time Setup

If you're a system administrator setting up the system for the first time:
1. Register your first device (see Section 3.1)
2. Add sensors to the device (see Section 3.2)
3. Configure alert rules (see Section 5.1)
4. Start monitoring (see Section 4.1)

## 3. Device Management

### 3.1 Registering a New Device

To register a new IoT device:

1. **Navigate to Devices Page**
   - Click "Devices" in the sidebar menu
   - Or navigate to `/devices` URL

2. **Click "Add Device" Button**
   - Located at the top right of the device list page

3. **Fill in Device Information**
   - **Device Name:** Enter a descriptive name (e.g., "Motor-001")
   - **Device Type:** Select or enter device type (e.g., "Motor", "Temperature Sensor")
   - **Location:** Enter device location (e.g., "Building A, Floor 2")
   - **Facility Type:** Select facility type (e.g., "Industrial", "Commercial")
   - **Edge Device Type:** Enter edge device type (e.g., "Raspberry Pi", "ESP32")
   - **Edge Device ID:** Enter unique edge device identifier
   - **Description:** (Optional) Add additional notes

4. **Click "Save" or "Create"**
   - Device will be created and appear in the device list

**Example:**
```
Device Name: Motor-001
Device Type: Motor
Location: Building A, Floor 2, Room 205
Facility Type: Industrial
Edge Device Type: Raspberry Pi
Edge Device ID: RPI-001
Description: Main motor for production line
```

### 3.2 Viewing Device List

1. **Navigate to Devices Page**
   - Click "Devices" in the sidebar

2. **View Device Cards**
   - Each device is displayed as a card showing:
     - Device name and type
     - Location
     - Status indicator (Online/Offline/Error)
     - Last seen timestamp

3. **Search and Filter**
   - Use search bar to find devices by name
   - Filter by device type, status, or location
   - Click on a device card to view details

### 3.3 Viewing Device Details

1. **Click on a Device Card**
   - Opens the device detail page

2. **Device Information Section**
   - Shows all device details
   - Displays current status
   - Shows last seen timestamp

3. **Sensors Section**
   - Lists all sensors associated with the device
   - Shows sensor name, type, and current value
   - Click on a sensor to view detailed readings

4. **Real-Time Charts Section**
   - Displays real-time charts for each sensor
   - Charts update automatically as new data arrives

### 3.4 Editing a Device

1. **Navigate to Device Detail Page**
   - Click on the device card

2. **Click "Edit" Button**
   - Located in the device information section

3. **Modify Device Information**
   - Update any fields as needed
   - Device name, location, description, etc.

4. **Click "Save"**
   - Changes are saved and reflected immediately

### 3.5 Deleting a Device

1. **Navigate to Device Detail Page**
   - Click on the device card

2. **Click "Delete" Button**
   - Usually located near the "Edit" button
   - May require confirmation

3. **Confirm Deletion**
   - Click "Yes" or "Confirm" to delete
   - Device will be removed from the system

**Warning:** Deleting a device may also delete associated sensors and historical data. Proceed with caution.

## 4. Sensor Management

### 4.1 Adding a Sensor to a Device

1. **Navigate to Device Detail Page**
   - Click on the device card

2. **Click "Add Sensor" Button**
   - Located in the Sensors section

3. **Fill in Sensor Information**
   - **Sensor Name:** Enter descriptive name (e.g., "Temperature", "Vibration")
   - **Sensor Type:** Select or enter type (e.g., "Temperature", "Humidity")
   - **Unit:** Enter measurement unit (e.g., "°C", "%", "g")
   - **Min Value:** (Optional) Minimum expected value
   - **Max Value:** (Optional) Maximum expected value
   - **Edge Device ID:** (Optional) If different from device's edge device

4. **Click "Save" or "Create"**
   - Sensor will be added to the device

**Example:**
```
Sensor Name: Temperature
Sensor Type: Temperature
Unit: °C
Min Value: -40
Max Value: 85
```

### 4.2 Viewing Sensor Readings

1. **Navigate to Device Detail Page**
   - Click on the device card

2. **View Sensor List**
   - All sensors are listed with current values
   - Real-time charts show data trends

3. **Click on a Sensor**
   - Opens sensor detail view
   - Shows historical readings
   - Displays detailed chart

### 4.3 Editing a Sensor

1. **Navigate to Device Detail Page**
   - Click on the device card

2. **Find the Sensor**
   - Locate sensor in the Sensors section

3. **Click "Edit" Button**
   - Usually on the sensor card

4. **Modify Sensor Information**
   - Update name, type, unit, or value ranges

5. **Click "Save"**
   - Changes are saved

## 5. Monitoring and Visualization

### 5.1 Dashboard Overview

The dashboard provides a comprehensive overview of your IoT monitoring system:

1. **Navigate to Dashboard**
   - Click "Dashboard" in the sidebar
   - Or navigate to home page (`/`)

2. **Device Status Overview**
   - Shows count of devices by status:
     - Online devices
     - Offline devices
     - Devices with errors
   - Visual indicators (colors, icons)

3. **Active Alerts Summary**
   - Displays count of active alerts
   - Shows alerts by severity (High, Medium, Low)
   - Click to view alert details

4. **Real-Time Charts**
   - Shows live data from key sensors
   - Updates automatically (no page refresh needed)
   - Multiple charts for different sensors

5. **Recent Activity**
   - Lists recent sensor readings
   - Shows recent alerts
   - Displays device status changes

### 5.2 Real-Time Data Monitoring

1. **View Real-Time Charts**
   - Charts on dashboard and device detail pages
   - Update automatically when new data arrives
   - Typically updates within 1-2 seconds

2. **Monitor Multiple Devices**
   - Open multiple browser tabs
   - Each tab can monitor different devices
   - All update in real-time

3. **Device Status Indicators**
   - **Green (Online):** Device is online and sending data
   - **Blue (Connected):** Device is connected but may not be sending data
   - **Gray (Offline):** Device is offline or not responding
   - **Red (Error):** Device has an error
   - **Yellow (Maintenance):** Device is in maintenance mode

### 5.3 Historical Data Viewing

1. **Navigate to Historical Data Page**
   - Click "Historical Data" in the sidebar
   - Or navigate to `/historical` URL

2. **Select Device and Sensor**
   - Choose device from dropdown
   - Choose sensor from dropdown

3. **Select Date Range**
   - Choose start date
   - Choose end date
   - Click "Apply" or "Load Data"

4. **View Historical Chart**
   - Chart displays data for selected range
   - Can zoom in/out
   - Can export data (if feature available)

5. **View Data Table (Optional)**
   - Switch to table view
   - See individual readings
   - Export to CSV (if feature available)

## 6. Alert Management

### 6.1 Creating Alert Rules

Alert rules define when alerts should be triggered:

1. **Navigate to Alert Rules Page**
   - Click "Alert Rules" in the sidebar
   - Or navigate to `/alertrules` URL

2. **Click "Create Alert Rule" Button**

3. **Fill in Rule Information**
   - **Rule Name:** Descriptive name (e.g., "High Temperature Alert")
   - **Device:** Select device (optional - for device-wide alerts)
   - **Sensor:** Select sensor (optional - for sensor-specific alerts)
   - **Rule Type:** Select type (e.g., "Threshold")
   - **Condition:** Describe condition (e.g., "Temperature exceeds threshold")
   - **Threshold Value:** Enter threshold value (e.g., 80.0)
   - **Comparison Operator:** Select operator (>, <, >=, <=, ==)
   - **Severity:** Select severity (Low, Medium, High, Critical)

4. **Click "Save" or "Create"**
   - Rule is created and active
   - Alerts will trigger when condition is met

**Example:**
```
Rule Name: High Temperature Alert
Device: Motor-001
Sensor: Temperature
Rule Type: Threshold
Condition: Temperature exceeds threshold
Threshold Value: 80.0
Comparison Operator: >
Severity: High
```

### 6.2 Viewing Alerts

1. **Navigate to Alerts Page**
   - Click "Alerts" in the sidebar
   - Or navigate to `/alerts` URL

2. **View Alert List**
   - All alerts are displayed
   - Shows alert details:
     - Alert message
     - Severity (color-coded)
     - Device and sensor
     - Triggered timestamp
     - Status (Active, Acknowledged, Resolved)

3. **Filter Alerts**
   - Filter by status (Active, Acknowledged, Resolved)
   - Filter by severity (Low, Medium, High, Critical)
   - Filter by device
   - Filter by date range

4. **Sort Alerts**
   - Sort by date (newest first)
   - Sort by severity
   - Sort by status

### 6.3 Acknowledging Alerts

When you acknowledge an alert, you indicate that you're aware of it:

1. **Navigate to Alerts Page**
   - Click "Alerts" in the sidebar

2. **Find the Alert**
   - Locate the alert in the list

3. **Click "Acknowledge" Button**
   - Usually on the alert card
   - Or in the alert detail view

4. **Alert Status Updates**
   - Status changes to "Acknowledged"
   - Timestamp is recorded
   - Alert remains visible but marked as acknowledged

### 6.4 Resolving Alerts

When an alert condition is resolved:

1. **Navigate to Alerts Page**
   - Click "Alerts" in the sidebar

2. **Find the Alert**
   - Locate the alert (Active or Acknowledged)

3. **Click "Resolve" Button**
   - Usually on the alert card

4. **Alert Status Updates**
   - Status changes to "Resolved"
   - Resolved timestamp is recorded
   - Alert may be filtered out of active alerts view

### 6.5 Alert Notifications

The system provides real-time alert notifications:

1. **Dashboard Notifications**
   - New alerts appear on dashboard
   - Visual indicators (badges, icons)
   - Alert count updates automatically

2. **Alert List Updates**
   - New alerts appear in real-time
   - No page refresh needed
   - Alerts are highlighted when new

3. **Browser Notifications (Optional)**
   - Browser may show desktop notifications
   - Requires browser permission
   - Can be enabled/disabled in browser settings

## 7. Troubleshooting

### 7.1 Common Issues

#### Issue: Device Shows as Offline
**Possible Causes:**
- Edge device is not powered on
- Network connection issue
- Edge device not sending data

**Solutions:**
1. Check if edge device is powered on
2. Verify network connection
3. Check edge device logs
4. Verify edge device is configured correctly
5. Check if device is sending data to API

#### Issue: No Data Appearing in Charts
**Possible Causes:**
- No sensor readings received
- Date range filter too narrow
- Sensor not configured correctly

**Solutions:**
1. Check if sensor readings are being sent
2. Verify date range includes current time
3. Check sensor configuration
4. Verify API is receiving data
5. Check browser console for errors

#### Issue: Alerts Not Triggering
**Possible Causes:**
- Alert rule not configured correctly
- Threshold not exceeded
- Alert rule disabled

**Solutions:**
1. Verify alert rule is enabled
2. Check threshold value
3. Verify sensor readings are being received
4. Check alert rule configuration
5. Test with a lower threshold temporarily

#### Issue: Real-Time Updates Not Working
**Possible Causes:**
- SignalR connection lost
- Browser WebSocket support
- Network/firewall blocking

**Solutions:**
1. Refresh the page
2. Check browser console for errors
3. Verify SignalR connection status
4. Check network/firewall settings
5. Try a different browser

### 7.2 Getting Help

If you encounter issues:

1. **Check System Status**
   - Verify system is running
   - Check if other users are experiencing issues

2. **Review Error Messages**
   - Read error messages carefully
   - Note error codes or messages

3. **Contact Support**
   - Contact system administrator
   - Provide error details
   - Describe steps to reproduce issue

## 8. Best Practices

### 8.1 Device Management
- Use descriptive device names
- Keep device information up to date
- Regularly review device status
- Remove unused devices

### 8.2 Sensor Configuration
- Set appropriate min/max values
- Use correct units
- Keep sensor information accurate
- Document sensor locations

### 8.3 Alert Management
- Set realistic thresholds
- Use appropriate severity levels
- Regularly review and update alert rules
- Acknowledge and resolve alerts promptly
- Remove unused alert rules

### 8.4 Monitoring
- Check dashboard regularly
- Review historical trends
- Monitor for patterns or anomalies
- Keep an eye on active alerts

## 9. Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `/` | Focus search bar |
| `Esc` | Close modal/dialog |
| `Ctrl+F` | Find on page |
| `F5` | Refresh page |

## 10. Glossary

- **Device:** Physical IoT equipment being monitored
- **Sensor:** Component that measures physical quantities
- **Edge Device:** Microcontroller or single-board computer that interfaces with sensors
- **Reading:** Single measurement value from a sensor
- **Alert:** Notification triggered when a condition is met
- **Alert Rule:** Configuration that defines when alerts should trigger
- **Threshold:** Value that triggers an alert when exceeded
- **Real-Time:** Data updates automatically without page refresh
- **Dashboard:** Main page showing system overview
- **Status:** Current state of a device (Online, Offline, Error, etc.)

## 11. Appendix

### 11.1 Supported Browsers
- Google Chrome (recommended)
- Microsoft Edge
- Mozilla Firefox
- Safari (macOS/iOS)

### 11.2 System Limits
- Maximum devices: Varies by system configuration
- Maximum sensors per device: Varies by system configuration
- Historical data retention: Configurable (default: 30 days)
- Real-time data points per chart: 50-100 (for performance)

### 11.3 Contact Information
- **System Administrator:** [Contact Information]
- **Support Email:** [Email Address]
- **Support Phone:** [Phone Number]

---

## Notes

This user manual is a living document and will be updated as the system evolves. For the latest information, consult your system administrator.

---

## Approval

- **Prepared by:** [Your Name]
- **Reviewed by:** [Reviewer Name]
- **Approved by:** [Approver Name]
- **Date:** [Date]

