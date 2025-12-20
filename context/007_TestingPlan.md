# Testing Plan

## Document Information
- **Project:** Web-Based IoT Device Real-Time Monitoring System
- **Version:** 1.0
- **Date:** 2026
- **Status:** Draft

## 1. Introduction

### 1.1 Purpose
This document outlines the testing strategy, test cases, and testing methodology for the Web-Based IoT Device Real-Time Monitoring System. It defines the testing approach, test types, test cases, and acceptance criteria to ensure the system meets all functional and non-functional requirements.

### 1.2 Scope
The testing plan covers:
- Unit testing (backend and frontend)
- Integration testing (API endpoints, database, SignalR)
- System testing (end-to-end functionality)
- User acceptance testing (UAT) scenarios
- Performance testing (basic)
- Security testing (basic validation)

**Note:** For capstone project scope, comprehensive performance and security testing may be limited. Focus will be on functional testing and basic non-functional testing.

### 1.3 Testing Objectives
- Verify all functional requirements are met
- Ensure system reliability and stability
- Validate data accuracy and integrity
- Confirm real-time communication works correctly
- Verify user interface functionality
- Ensure proper error handling

## 2. Testing Strategy

### 2.1 Testing Levels

#### 2.1.1 Unit Testing
- **Scope:** Individual components, functions, and methods
- **Backend:** C# unit tests using xUnit or NUnit
- **Frontend:** React component tests using Jest and React Testing Library
- **Coverage Target:** 70-80% code coverage (capstone scope)

#### 2.1.2 Integration Testing
- **Scope:** API endpoints, database interactions, SignalR connections
- **Tools:** xUnit for backend, Postman/Newman for API testing
- **Focus:** Verify components work together correctly

#### 2.1.3 System Testing
- **Scope:** End-to-end functionality
- **Tools:** Manual testing, automated E2E tests (optional)
- **Focus:** Complete user workflows

#### 2.1.4 User Acceptance Testing (UAT)
- **Scope:** Validate system meets user requirements
- **Approach:** Manual testing with test scenarios
- **Focus:** Real-world usage scenarios

### 2.2 Testing Types

| Test Type | Description | Priority |
|-----------|-------------|----------|
| Functional Testing | Verify all features work as specified | High |
| Integration Testing | Verify components work together | High |
| UI/UX Testing | Verify user interface functionality | High |
| Real-Time Testing | Verify SignalR communication | High |
| Data Validation Testing | Verify data accuracy and integrity | High |
| Error Handling Testing | Verify error scenarios | Medium |
| Performance Testing | Basic performance validation | Medium |
| Security Testing | Basic input validation and security | Low |

## 3. Test Environment

### 3.1 Development Environment
- **Backend:** Local development server (localhost:5001)
- **Frontend:** Local development server (localhost:3000)
- **Database:** SQL Server LocalDB or SQL Server Express
- **Edge Devices:** Simulated using HTTP client or test scripts

### 3.2 Test Data
- Pre-populated test devices (5-10 devices)
- Pre-populated test sensors (10-20 sensors)
- Test sensor readings (historical data for testing)
- Test alert rules
- Test alerts (various statuses)

### 3.3 Test Tools

#### Backend Testing
- **Unit Testing:** xUnit or NUnit
- **Mocking:** Moq
- **API Testing:** Postman, Swagger UI
- **Database Testing:** In-memory database or test database

#### Frontend Testing
- **Unit Testing:** Jest
- **Component Testing:** React Testing Library
- **E2E Testing:** Cypress or Playwright (optional)

## 4. Test Cases

### 4.1 Device Management Test Cases

#### TC-DM-001: Create Device
**Test ID:** TC-DM-001  
**Test Type:** Integration  
**Priority:** High  
**Description:** Verify that a new device can be created successfully

**Preconditions:**
- API is running
- Database is accessible

**Test Steps:**
1. Send POST request to `/api/v1/devices` with valid device data
2. Verify response status code is 201
3. Verify response contains device data with generated deviceId
4. Query database to verify device was created
5. Send GET request to retrieve the created device

**Test Data:**
```json
{
  "deviceName": "Test Motor-001",
  "deviceType": "Motor",
  "location": "Test Building, Floor 1",
  "facilityType": "Industrial",
  "edgeDeviceType": "Raspberry Pi",
  "edgeDeviceId": "TEST-RPI-001"
}
```

**Expected Results:**
- Status code: 201 Created
- Device ID is generated
- All fields are saved correctly
- Device can be retrieved via GET request

**Actual Results:** [To be filled during testing]

---

#### TC-DM-002: Create Device - Validation Error
**Test ID:** TC-DM-002  
**Test Type:** Integration  
**Priority:** High  
**Description:** Verify that creating a device with invalid data returns validation errors

**Test Steps:**
1. Send POST request with missing required field (deviceName)
2. Verify response status code is 400
3. Verify response contains error messages

**Test Data:**
```json
{
  "deviceType": "Motor"
}
```

**Expected Results:**
- Status code: 400 Bad Request
- Error message indicates missing required field

---

#### TC-DM-003: Get All Devices
**Test ID:** TC-DM-003  
**Test Type:** Integration  
**Priority:** High  
**Description:** Verify that all devices can be retrieved

**Preconditions:**
- At least 3 devices exist in database

**Test Steps:**
1. Send GET request to `/api/v1/devices`
2. Verify response status code is 200
3. Verify response contains array of devices
4. Verify all devices have required fields

**Expected Results:**
- Status code: 200 OK
- Response contains array of devices
- Each device has all required fields

---

#### TC-DM-004: Get Device by ID
**Test ID:** TC-DM-004  
**Test Type:** Integration  
**Priority:** High  
**Description:** Verify that a specific device can be retrieved by ID

**Preconditions:**
- Device with ID 1 exists

**Test Steps:**
1. Send GET request to `/api/v1/devices/1`
2. Verify response status code is 200
3. Verify response contains correct device data

**Expected Results:**
- Status code: 200 OK
- Response contains device with ID 1
- All device fields are present

---

#### TC-DM-005: Get Device by ID - Not Found
**Test ID:** TC-DM-005  
**Test Type:** Integration  
**Priority:** Medium  
**Description:** Verify that requesting non-existent device returns 404

**Test Steps:**
1. Send GET request to `/api/v1/devices/99999`
2. Verify response status code is 404
3. Verify response contains error message

**Expected Results:**
- Status code: 404 Not Found
- Error message indicates device not found

---

#### TC-DM-006: Update Device
**Test ID:** TC-DM-006  
**Test Type:** Integration  
**Priority:** High  
**Description:** Verify that a device can be updated

**Preconditions:**
- Device with ID 1 exists

**Test Steps:**
1. Send PUT request to `/api/v1/devices/1` with updated data
2. Verify response status code is 200
3. Verify response contains updated device data
4. Send GET request to verify changes persisted

**Test Data:**
```json
{
  "deviceName": "Updated Motor-001",
  "location": "Updated Location"
}
```

**Expected Results:**
- Status code: 200 OK
- Device data is updated
- Changes are persisted in database

---

#### TC-DM-007: Delete Device
**Test ID:** TC-DM-007  
**Test Type:** Integration  
**Priority:** High  
**Description:** Verify that a device can be deleted

**Preconditions:**
- Device with ID 1 exists

**Test Steps:**
1. Send DELETE request to `/api/v1/devices/1`
2. Verify response status code is 204
3. Send GET request to verify device no longer exists (or isActive=false)

**Expected Results:**
- Status code: 204 No Content
- Device is deleted or marked as inactive

---

### 4.2 Sensor Management Test Cases

#### TC-SM-001: Create Sensor
**Test ID:** TC-SM-001  
**Test Type:** Integration  
**Priority:** High  
**Description:** Verify that a new sensor can be created for a device

**Preconditions:**
- Device with ID 1 exists

**Test Steps:**
1. Send POST request to `/api/v1/devices/1/sensors` with valid sensor data
2. Verify response status code is 201
3. Verify response contains sensor data with generated sensorId
4. Query database to verify sensor was created

**Test Data:**
```json
{
  "sensorName": "Temperature Sensor",
  "sensorType": "Temperature",
  "unit": "°C",
  "minValue": -40.0,
  "maxValue": 85.0
}
```

**Expected Results:**
- Status code: 201 Created
- Sensor ID is generated
- Sensor is associated with correct device

---

#### TC-SM-002: Get Sensors by Device
**Test ID:** TC-SM-002  
**Test Type:** Integration  
**Priority:** High  
**Description:** Verify that all sensors for a device can be retrieved

**Preconditions:**
- Device with ID 1 exists
- Device has at least 2 sensors

**Test Steps:**
1. Send GET request to `/api/v1/devices/1/sensors`
2. Verify response status code is 200
3. Verify response contains array of sensors
4. Verify all sensors belong to device 1

**Expected Results:**
- Status code: 200 OK
- Response contains array of sensors
- All sensors have correct deviceId

---

### 4.3 Sensor Readings Test Cases

#### TC-SR-001: Create Sensor Reading
**Test ID:** TC-SR-001  
**Test Type:** Integration  
**Priority:** High  
**Description:** Verify that a sensor reading can be created (primary endpoint for edge devices)

**Preconditions:**
- Device with ID 1 exists
- Sensor with ID 1 exists and belongs to device 1

**Test Steps:**
1. Send POST request to `/api/v1/sensorreadings` with valid reading data
2. Verify response status code is 201
3. Verify response contains reading data with generated readingId
4. Query database to verify reading was created
5. Verify SignalR event is triggered (if SignalR is connected)

**Test Data:**
```json
{
  "deviceId": 1,
  "sensorId": 1,
  "value": 25.5,
  "timestamp": "2026-01-20T14:30:00Z"
}
```

**Expected Results:**
- Status code: 201 Created
- Reading ID is generated
- Reading is stored in database
- SignalR event is broadcast (if applicable)

---

#### TC-SR-002: Batch Create Sensor Readings
**Test ID:** TC-SR-002  
**Test Type:** Integration  
**Priority:** High  
**Description:** Verify that multiple sensor readings can be created in batch

**Preconditions:**
- Device with ID 1 exists
- Sensor with ID 1 and 2 exist

**Test Steps:**
1. Send POST request to `/api/v1/sensorreadings/batch` with array of readings
2. Verify response status code is 201
3. Verify response indicates correct number of readings created
4. Query database to verify all readings were created

**Test Data:**
```json
{
  "readings": [
    {
      "deviceId": 1,
      "sensorId": 1,
      "value": 25.5,
      "timestamp": "2026-01-20T14:30:00Z"
    },
    {
      "deviceId": 1,
      "sensorId": 2,
      "value": 0.5,
      "timestamp": "2026-01-20T14:30:00Z"
    }
  ]
}
```

**Expected Results:**
- Status code: 201 Created
- All readings are created
- Response indicates createdCount = 2

---

#### TC-SR-003: Query Sensor Readings with Filters
**Test ID:** TC-SR-003  
**Test Type:** Integration  
**Priority:** High  
**Description:** Verify that sensor readings can be queried with date range and filters

**Preconditions:**
- Device with ID 1 exists
- Sensor with ID 1 exists
- At least 10 readings exist for sensor 1 within date range

**Test Steps:**
1. Send GET request to `/api/v1/sensorreadings?deviceId=1&sensorId=1&startDate=2026-01-20T00:00:00Z&endDate=2026-01-20T23:59:59Z`
2. Verify response status code is 200
3. Verify response contains paginated results
4. Verify all readings are within date range
5. Verify pagination metadata is correct

**Expected Results:**
- Status code: 200 OK
- Response contains readings within date range
- Pagination metadata is correct

---

### 4.4 Alert Management Test Cases

#### TC-AL-001: Create Alert Rule
**Test ID:** TC-AL-001  
**Test Type:** Integration  
**Priority:** High  
**Description:** Verify that an alert rule can be created

**Preconditions:**
- Device with ID 1 exists
- Sensor with ID 1 exists

**Test Steps:**
1. Send POST request to `/api/v1/alertrules` with valid rule data
2. Verify response status code is 201
3. Verify response contains rule data with generated alertRuleId
4. Query database to verify rule was created

**Test Data:**
```json
{
  "deviceId": 1,
  "sensorId": 1,
  "ruleName": "High Temperature Alert",
  "ruleType": "Threshold",
  "condition": "Temperature exceeds threshold",
  "thresholdValue": 80.0,
  "comparisonOperator": ">",
  "severity": "High"
}
```

**Expected Results:**
- Status code: 201 Created
- Alert rule ID is generated
- Rule is stored in database

---

#### TC-AL-002: Alert Triggered on Threshold Breach
**Test ID:** TC-AL-002  
**Test Type:** Integration  
**Priority:** High  
**Description:** Verify that an alert is triggered when sensor reading exceeds threshold

**Preconditions:**
- Device with ID 1 exists
- Sensor with ID 1 exists
- Alert rule exists: thresholdValue = 80.0, operator = ">"

**Test Steps:**
1. Send POST request to create sensor reading with value = 85.5
2. Verify alert evaluation logic runs
3. Verify alert is created in database
4. Verify SignalR event "AlertTriggered" is broadcast
5. Query alerts endpoint to verify alert exists

**Test Data:**
```json
{
  "deviceId": 1,
  "sensorId": 1,
  "value": 85.5,
  "timestamp": "2026-01-20T14:30:00Z"
}
```

**Expected Results:**
- Alert is created
- Alert status is "Active"
- Alert severity matches rule severity
- SignalR event is broadcast

---

#### TC-AL-003: Get Active Alerts
**Test ID:** TC-AL-003  
**Test Type:** Integration  
**Priority:** High  
**Description:** Verify that active alerts can be retrieved

**Preconditions:**
- At least 2 active alerts exist
- At least 1 resolved alert exists

**Test Steps:**
1. Send GET request to `/api/v1/alerts/active`
2. Verify response status code is 200
3. Verify response contains only active alerts
4. Verify resolved alerts are not included

**Expected Results:**
- Status code: 200 OK
- Response contains only active alerts
- Resolved alerts are excluded

---

#### TC-AL-004: Acknowledge Alert
**Test ID:** TC-AL-004  
**Test Type:** Integration  
**Priority:** High  
**Description:** Verify that an alert can be acknowledged

**Preconditions:**
- Active alert with ID 1001 exists

**Test Steps:**
1. Send PUT request to `/api/v1/alerts/1001/acknowledge`
2. Verify response status code is 200
3. Verify alert status is "Acknowledged"
4. Verify acknowledgedAt timestamp is set
5. Verify SignalR event "AlertAcknowledged" is broadcast

**Expected Results:**
- Status code: 200 OK
- Alert status is "Acknowledged"
- acknowledgedAt is set
- SignalR event is broadcast

---

#### TC-AL-005: Resolve Alert
**Test ID:** TC-AL-005  
**Test Type:** Integration  
**Priority:** High  
**Description:** Verify that an alert can be resolved

**Preconditions:**
- Active or acknowledged alert with ID 1001 exists

**Test Steps:**
1. Send PUT request to `/api/v1/alerts/1001/resolve`
2. Verify response status code is 200
3. Verify alert status is "Resolved"
4. Verify resolvedAt timestamp is set
5. Verify SignalR event "AlertResolved" is broadcast

**Expected Results:**
- Status code: 200 OK
- Alert status is "Resolved"
- resolvedAt is set
- SignalR event is broadcast

---

### 4.5 Real-Time Communication Test Cases

#### TC-RT-001: SignalR Connection
**Test ID:** TC-RT-001  
**Test Type:** Integration  
**Priority:** High  
**Description:** Verify that SignalR connection can be established

**Test Steps:**
1. Connect to SignalR hub at `/hubs/monitoring`
2. Verify connection is established
3. Verify connection state is "Connected"
4. Disconnect and verify connection closes

**Expected Results:**
- Connection is established successfully
- Connection state is "Connected"
- Disconnection works correctly

---

#### TC-RT-002: Subscribe to Device Updates
**Test ID:** TC-RT-002  
**Test Type:** Integration  
**Priority:** High  
**Description:** Verify that client can subscribe to device updates

**Preconditions:**
- SignalR connection is established
- Device with ID 1 exists

**Test Steps:**
1. Connect to SignalR hub
2. Invoke "SubscribeToDevice" with deviceId = 1
3. Create a sensor reading for device 1
4. Verify "SensorReadingReceived" event is received
5. Verify event data contains correct reading information

**Expected Results:**
- Subscription is successful
- Event is received when reading is created
- Event data is correct

---

#### TC-RT-003: Device Status Change Notification
**Test ID:** TC-RT-003  
**Test Type:** Integration  
**Priority:** High  
**Description:** Verify that device status changes trigger SignalR events

**Preconditions:**
- SignalR connection is established
- Client is subscribed to device 1
- Device with ID 1 exists

**Test Steps:**
1. Connect and subscribe to device 1
2. Update device status (via API or service)
3. Verify "DeviceStatusChanged" event is received
4. Verify event data contains new status

**Expected Results:**
- Event is received when status changes
- Event data contains correct status information

---

### 4.6 Frontend Test Cases

#### TC-FE-001: Device List Page Loads
**Test ID:** TC-FE-001  
**Test Type:** System  
**Priority:** High  
**Description:** Verify that device list page loads and displays devices

**Test Steps:**
1. Navigate to `/devices` page
2. Verify page loads without errors
3. Verify device list is displayed
4. Verify each device card shows required information
5. Verify loading spinner appears during data fetch

**Expected Results:**
- Page loads successfully
- Device list is displayed
- Device cards show correct information
- Loading states work correctly

---

#### TC-FE-002: Create Device from UI
**Test ID:** TC-FE-002  
**Test Type:** System  
**Priority:** High  
**Description:** Verify that a device can be created from the user interface

**Test Steps:**
1. Navigate to device list page
2. Click "Add Device" button
3. Fill in device form with valid data
4. Submit form
5. Verify success message appears
6. Verify new device appears in device list
7. Verify form validation works (try submitting empty form)

**Expected Results:**
- Form opens correctly
- Form validation works
- Device is created successfully
- Success message is displayed
- Device appears in list

---

#### TC-FE-003: Real-Time Chart Updates
**Test ID:** TC-FE-003  
**Test Type:** System  
**Priority:** High  
**Description:** Verify that real-time charts update when new readings arrive

**Preconditions:**
- Device detail page is open
- Real-time chart is displayed
- SignalR connection is established

**Test Steps:**
1. Navigate to device detail page
2. Verify real-time chart is displayed
3. Send sensor reading via API (simulate edge device)
4. Verify chart updates with new data point
5. Verify chart maintains reasonable number of data points (doesn't grow indefinitely)

**Expected Results:**
- Chart displays correctly
- Chart updates when new reading arrives
- Chart maintains performance (limits data points)

---

#### TC-FE-004: Alert Notification Display
**Test ID:** TC-FE-004  
**Test Type:** System  
**Priority:** High  
**Description:** Verify that alerts are displayed in real-time

**Preconditions:**
- Alerts page is open
- SignalR connection is established

**Test Steps:**
1. Navigate to alerts page
2. Trigger an alert (create reading that breaches threshold)
3. Verify alert appears in alerts list
4. Verify alert has correct severity indicator
5. Verify alert can be acknowledged from UI
6. Verify alert status updates in real-time

**Expected Results:**
- Alert appears in list
- Alert has correct visual indicators
- Alert can be acknowledged
- Status updates in real-time

---

### 4.7 Data Validation Test Cases

#### TC-DV-001: Invalid Sensor Reading Value
**Test ID:** TC-DV-001  
**Test Type:** Integration  
**Priority:** Medium  
**Description:** Verify that invalid sensor reading values are rejected

**Test Steps:**
1. Send POST request with invalid value (e.g., null, string, out of range)
2. Verify response status code is 400
3. Verify response contains validation error

**Test Data:**
```json
{
  "deviceId": 1,
  "sensorId": 1,
  "value": "invalid"
}
```

**Expected Results:**
- Status code: 400 Bad Request
- Validation error message is returned

---

#### TC-DV-002: Missing Required Fields
**Test ID:** TC-DV-002  
**Test Type:** Integration  
**Priority:** Medium  
**Description:** Verify that requests with missing required fields are rejected

**Test Steps:**
1. Send POST request to create device without deviceName
2. Verify response status code is 400
3. Verify response lists missing required fields

**Expected Results:**
- Status code: 400 Bad Request
- Error message lists missing fields

---

### 4.8 Error Handling Test Cases

#### TC-EH-001: Database Connection Error
**Test ID:** TC-EH-001  
**Test Type:** Integration  
**Priority:** Medium  
**Description:** Verify that database connection errors are handled gracefully

**Test Steps:**
1. Stop database service
2. Send GET request to `/api/v1/devices`
3. Verify response status code is 500
4. Verify response contains error message (not exposing internal details)
5. Restart database and verify system recovers

**Expected Results:**
- Status code: 500 Internal Server Error
- Error message is user-friendly
- System recovers when database is available

---

#### TC-EH-002: Invalid Endpoint
**Test ID:** TC-EH-002  
**Test Type:** Integration  
**Priority:** Low  
**Description:** Verify that invalid endpoints return 404

**Test Steps:**
1. Send GET request to `/api/v1/invalid-endpoint`
2. Verify response status code is 404
3. Verify response contains error message

**Expected Results:**
- Status code: 404 Not Found
- Error message indicates endpoint not found

---

## 5. Test Execution Plan

### 5.1 Testing Phases

#### Phase 1: Unit Testing (Week 1-2)
- Write and execute unit tests for backend services
- Write and execute unit tests for frontend components
- Target: 70% code coverage

#### Phase 2: Integration Testing (Week 3)
- Test API endpoints
- Test database interactions
- Test SignalR connections
- Test frontend-backend integration

#### Phase 3: System Testing (Week 4)
- End-to-end functional testing
- UI/UX testing
- Real-time communication testing
- Error scenario testing

#### Phase 4: User Acceptance Testing (Week 5)
- Execute UAT scenarios
- Gather feedback
- Fix critical issues

### 5.2 Test Execution Schedule

| Week | Phase | Activities |
|------|-------|------------|
| 1-2 | Unit Testing | Backend and frontend unit tests |
| 3 | Integration Testing | API, database, SignalR tests |
| 4 | System Testing | E2E testing, UI testing |
| 5 | UAT | User acceptance testing |

### 5.3 Test Data Management

- **Test Database:** Separate test database for integration tests
- **Test Data Setup:** Scripts to populate test data
- **Test Data Cleanup:** Scripts to clean up after tests
- **Data Isolation:** Each test should be independent

## 6. Test Metrics and Reporting

### 6.1 Test Metrics

- **Test Coverage:** Percentage of code covered by tests
- **Test Pass Rate:** Percentage of tests passing
- **Defect Density:** Number of defects per test case
- **Defect Resolution Time:** Time to fix defects

### 6.2 Test Reporting

- **Daily Test Status:** Summary of tests executed and results
- **Weekly Test Report:** Summary of testing progress
- **Defect Report:** List of defects found and their status
- **Final Test Report:** Comprehensive test results and summary

## 7. Defect Management

### 7.1 Defect Severity Levels

| Severity | Description | Example |
|----------|-------------|---------|
| Critical | System crash, data loss | Database connection failure |
| High | Major functionality broken | Cannot create devices |
| Medium | Minor functionality issue | Validation error message unclear |
| Low | Cosmetic issue | Button alignment |

### 7.2 Defect Lifecycle

1. **Open:** Defect is discovered and logged
2. **Assigned:** Defect is assigned to developer
3. **In Progress:** Developer is working on fix
4. **Fixed:** Fix is implemented
5. **Verified:** Fix is verified by tester
6. **Closed:** Defect is resolved and closed

## 8. Acceptance Criteria

### 8.1 Functional Acceptance

- ✅ All high-priority functional requirements are implemented and tested
- ✅ All API endpoints work correctly
- ✅ Real-time communication (SignalR) works correctly
- ✅ Data is stored and retrieved correctly
- ✅ Alerts are triggered and managed correctly

### 8.2 Non-Functional Acceptance

- ✅ System handles expected load (capstone scope)
- ✅ Basic error handling works correctly
- ✅ User interface is functional and responsive
- ✅ Basic security measures are in place (input validation)

## 9. Test Deliverables

- Test Plan (this document)
- Test Cases (detailed test cases)
- Test Scripts (automated test scripts)
- Test Data (test datasets)
- Test Results (test execution results)
- Defect Reports (defect tracking)
- Test Summary Report (final test report)

## 10. Notes

- For capstone project, focus on functional testing and basic integration testing
- Comprehensive performance and security testing may be limited
- Manual testing is acceptable for capstone scope
- Automated testing is encouraged but not required for all scenarios
- Test coverage target of 70-80% is reasonable for capstone

---

## Approval

- **Prepared by:** [Your Name]
- **Reviewed by:** [Reviewer Name]
- **Approved by:** [Approver Name]
- **Date:** [Date]

---

## Notes

This document is a living document and will be updated as testing progresses and new test cases are identified.

