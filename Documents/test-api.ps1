# API Testing Script
# Run this script after starting the API with: dotnet run --project IoTMonitoringSystem.API/IoTMonitoringSystem.API.csproj

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  IoT Monitoring System API Tests" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:5000/api/v1"
$deviceId = $null
$sensorId = $null
$alertRuleId = $null
$alertId = $null

# Test 1: Check API Health
Write-Host "=== TEST 1: Check API Health ===" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/devices" -Method Get -ContentType "application/json" -ErrorAction Stop
    Write-Host "✅ API is responding!" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "❌ API is not responding. Make sure the API is running!" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "To start the API, run:" -ForegroundColor Yellow
    Write-Host "   dotnet run --project IoTMonitoringSystem.API/IoTMonitoringSystem.API.csproj" -ForegroundColor White
    exit 1
}

# Test 2: Create Device
Write-Host "=== TEST 2: Create Device ===" -ForegroundColor Yellow
$deviceBody = @{
    deviceName = "Test Temperature Sensor"
    deviceType = "Temperature"
    location = "Test Lab - Room 101"
    facilityType = "Laboratory"
    edgeDeviceType = "ESP32"
    edgeDeviceId = "ESP32-TEST-001"
    description = "Test device for API validation"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/devices" -Method Post -Body $deviceBody -ContentType "application/json" -ErrorAction Stop
    if ($response.success) {
        $deviceId = $response.data.deviceId
        Write-Host "✅ Device created successfully!" -ForegroundColor Green
        Write-Host "   Device ID: $deviceId" -ForegroundColor White
        Write-Host "   Device Name: $($response.data.deviceName)" -ForegroundColor White
    } else {
        Write-Host "❌ Failed to create device" -ForegroundColor Red
        $response | ConvertTo-Json | Write-Host
    }
} catch {
    Write-Host "❌ Error creating device: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "   Response: $responseBody" -ForegroundColor Red
    }
}
Write-Host ""

# Test 3: Get All Devices
Write-Host "=== TEST 3: Get All Devices ===" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/devices" -Method Get -ContentType "application/json" -ErrorAction Stop
    if ($response.success) {
        Write-Host "✅ Retrieved devices successfully!" -ForegroundColor Green
        Write-Host "   Total devices: $($response.data.Count)" -ForegroundColor White
        if (-not $deviceId -and $response.data.Count -gt 0) {
            $deviceId = $response.data[0].deviceId
            Write-Host "   Using existing device ID: $deviceId" -ForegroundColor White
        }
    }
} catch {
    Write-Host "❌ Error retrieving devices: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

if (-not $deviceId) {
    Write-Host "❌ Cannot continue tests without a device ID" -ForegroundColor Red
    exit 1
}

# Test 4: Create Sensor
Write-Host "=== TEST 4: Create Sensor ===" -ForegroundColor Yellow
$sensorBody = @{
    sensorName = "Temperature Sensor"
    sensorType = "Temperature"
    unit = "°C"
    minValue = -40
    maxValue = 85
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/devices/$deviceId/sensors" -Method Post -Body $sensorBody -ContentType "application/json" -ErrorAction Stop
    if ($response.success) {
        $sensorId = $response.data.sensorId
        Write-Host "✅ Sensor created successfully!" -ForegroundColor Green
        Write-Host "   Sensor ID: $sensorId" -ForegroundColor White
        Write-Host "   Sensor Name: $($response.data.sensorName)" -ForegroundColor White
    } else {
        Write-Host "❌ Failed to create sensor" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Error creating sensor: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 5: Get Sensors for Device
Write-Host "=== TEST 5: Get Sensors for Device ===" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/devices/$deviceId/sensors" -Method Get -ContentType "application/json" -ErrorAction Stop
    if ($response.success) {
        Write-Host "✅ Retrieved sensors successfully!" -ForegroundColor Green
        Write-Host "   Total sensors: $($response.data.Count)" -ForegroundColor White
        if (-not $sensorId -and $response.data.Count -gt 0) {
            $sensorId = $response.data[0].sensorId
            Write-Host "   Using existing sensor ID: $sensorId" -ForegroundColor White
        }
    }
} catch {
    Write-Host "❌ Error retrieving sensors: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

if (-not $sensorId) {
    Write-Host "❌ Cannot continue tests without a sensor ID" -ForegroundColor Red
    exit 1
}

# Test 6: Create Alert Rule
Write-Host "=== TEST 6: Create Alert Rule ===" -ForegroundColor Yellow
$alertRuleBody = @{
    deviceId = $deviceId
    sensorId = $sensorId
    ruleName = "High Temperature Alert"
    ruleType = "threshold"
    condition = "Temperature exceeds 30 degrees Celsius"
    thresholdValue = 30.0
    comparisonOperator = ">"
    severity = "High"
    isEnabled = $true
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/alertrules" -Method Post -Body $alertRuleBody -ContentType "application/json" -ErrorAction Stop
    if ($response.success) {
        $alertRuleId = $response.data.alertRuleId
        Write-Host "✅ Alert rule created successfully!" -ForegroundColor Green
        Write-Host "   Alert Rule ID: $alertRuleId" -ForegroundColor White
        Write-Host "   Rule Name: $($response.data.ruleName)" -ForegroundColor White
        Write-Host "   Threshold: $($response.data.thresholdValue)" -ForegroundColor White
    } else {
        Write-Host "❌ Failed to create alert rule" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Error creating alert rule: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 7: Create Sensor Reading (Normal - Should NOT trigger alert)
Write-Host "=== TEST 7: Create Sensor Reading (Normal Value) ===" -ForegroundColor Yellow
$readingBody1 = @{
    deviceId = $deviceId
    sensorId = $sensorId
    value = 25.0
    timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    status = "Good"
    quality = "High"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/sensorreadings" -Method Post -Body $readingBody1 -ContentType "application/json" -ErrorAction Stop
    if ($response.success) {
        Write-Host "✅ Sensor reading created successfully!" -ForegroundColor Green
        Write-Host "   Reading ID: $($response.data.readingId)" -ForegroundColor White
        Write-Host "   Value: $($response.data.value)°C" -ForegroundColor White
        Write-Host "   Note: This should NOT trigger an alert (25.0 < 30.0)" -ForegroundColor Cyan
    }
} catch {
    Write-Host "❌ Error creating sensor reading: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 8: Create Sensor Reading (High - Should trigger alert)
Write-Host "=== TEST 8: Create Sensor Reading (High Value - Should Trigger Alert) ===" -ForegroundColor Yellow
Start-Sleep -Seconds 1
$readingBody2 = @{
    deviceId = $deviceId
    sensorId = $sensorId
    value = 35.5
    timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    status = "Good"
    quality = "High"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/sensorreadings" -Method Post -Body $readingBody2 -ContentType "application/json" -ErrorAction Stop
    if ($response.success) {
        Write-Host "✅ Sensor reading created successfully!" -ForegroundColor Green
        Write-Host "   Reading ID: $($response.data.readingId)" -ForegroundColor White
        Write-Host "   Value: $($response.data.value)°C" -ForegroundColor White
        Write-Host "   ⚠️  This SHOULD trigger an alert (35.5 > 30.0)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Error creating sensor reading: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 9: Check Active Alerts
Write-Host "=== TEST 9: Check Active Alerts ===" -ForegroundColor Yellow
Start-Sleep -Seconds 2
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/alerts/active" -Method Get -ContentType "application/json" -ErrorAction Stop
    if ($response.success) {
        $alertCount = $response.data.Count
        Write-Host "✅ Retrieved active alerts successfully!" -ForegroundColor Green
        Write-Host "   Active alerts count: $alertCount" -ForegroundColor White
        
        if ($alertCount -gt 0) {
            $alert = $response.data[0]
            $alertId = $alert.alertId
            Write-Host ""
            Write-Host "   First Alert Details:" -ForegroundColor Cyan
            Write-Host "     Alert ID: $($alert.alertId)" -ForegroundColor White
            Write-Host "     Message: $($alert.message)" -ForegroundColor White
            Write-Host "     Severity: $($alert.severity)" -ForegroundColor White
            Write-Host "     Status: $($alert.status)" -ForegroundColor White
            Write-Host "     Device ID: $($alert.deviceId)" -ForegroundColor White
            Write-Host "     Trigger Value: $($alert.triggerValue)" -ForegroundColor White
        } else {
            Write-Host "   ⚠️  No active alerts found" -ForegroundColor Yellow
        }
    }
} catch {
    Write-Host "❌ Error retrieving alerts: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 10: Get All Alerts (with pagination)
Write-Host "=== TEST 10: Get All Alerts (with pagination) ===" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/alerts?pageNumber=1&pageSize=10" -Method Get -ContentType "application/json" -ErrorAction Stop
    if ($response.success) {
        Write-Host "✅ Retrieved alerts successfully!" -ForegroundColor Green
        Write-Host "   Total alerts: $($response.data.totalCount)" -ForegroundColor White
        Write-Host "   Page: $($response.data.pageNumber) of $($response.data.totalPages)" -ForegroundColor White
        Write-Host "   Items on this page: $($response.data.items.Count)" -ForegroundColor White
    }
} catch {
    Write-Host "❌ Error retrieving alerts: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 11: Acknowledge Alert (if alert exists)
if ($alertId) {
    Write-Host "=== TEST 11: Acknowledge Alert ===" -ForegroundColor Yellow
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/alerts/$alertId/acknowledge" -Method Put -ContentType "application/json" -ErrorAction Stop
        if ($response.success) {
            Write-Host "✅ Alert acknowledged successfully!" -ForegroundColor Green
            Write-Host "   Alert ID: $alertId" -ForegroundColor White
            Write-Host "   Acknowledged At: $($response.data.acknowledgedAt)" -ForegroundColor White
        }
    } catch {
        Write-Host "❌ Error acknowledging alert: $($_.Exception.Message)" -ForegroundColor Red
    }
    Write-Host ""
}

# Test 12: Resolve Alert (if alert exists)
if ($alertId) {
    Write-Host "=== TEST 12: Resolve Alert ===" -ForegroundColor Yellow
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/alerts/$alertId/resolve" -Method Put -ContentType "application/json" -ErrorAction Stop
        if ($response.success) {
            Write-Host "✅ Alert resolved successfully!" -ForegroundColor Green
            Write-Host "   Alert ID: $alertId" -ForegroundColor White
            Write-Host "   Status: $($response.data.status)" -ForegroundColor White
            Write-Host "   Resolved At: $($response.data.resolvedAt)" -ForegroundColor White
        }
    } catch {
        Write-Host "❌ Error resolving alert: $($_.Exception.Message)" -ForegroundColor Red
    }
    Write-Host ""
}

# Test 13: Get Sensor Readings
Write-Host "=== TEST 13: Get Sensor Readings ===" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/sensorreadings?deviceId=$deviceId&pageNumber=1&pageSize=10" -Method Get -ContentType "application/json" -ErrorAction Stop
    if ($response.success) {
        Write-Host "✅ Retrieved sensor readings successfully!" -ForegroundColor Green
        Write-Host "   Total readings: $($response.data.totalCount)" -ForegroundColor White
        Write-Host "   Readings on this page: $($response.data.items.Count)" -ForegroundColor White
        if ($response.data.items.Count -gt 0) {
            $reading = $response.data.items[0]
            Write-Host ""
            Write-Host "   Latest reading:" -ForegroundColor Cyan
            Write-Host "     Reading ID: $($reading.readingId)" -ForegroundColor White
            Write-Host "     Value: $($reading.value)°C" -ForegroundColor White
            Write-Host "     Timestamp: $($reading.timestamp)" -ForegroundColor White
        }
    }
} catch {
    Write-Host "❌ Error retrieving readings: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 14: Get Device Status
Write-Host "=== TEST 14: Get Device Status ===" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/devices/$deviceId/status" -Method Get -ContentType "application/json" -ErrorAction Stop
    if ($response.success) {
        Write-Host "✅ Retrieved device status successfully!" -ForegroundColor Green
        Write-Host "   Device ID: $($response.data.deviceId)" -ForegroundColor White
        Write-Host "   Status: $($response.data.status)" -ForegroundColor White
        Write-Host "   Last Seen: $($response.data.lastSeenAt)" -ForegroundColor White
    }
} catch {
    Write-Host "❌ Error retrieving device status: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ All API tests completed!" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. View API in Swagger UI:" -ForegroundColor White
Write-Host "     http://localhost:5000/swagger" -ForegroundColor Cyan
Write-Host "     or" -ForegroundColor White
Write-Host "     https://localhost:5001/swagger" -ForegroundColor Cyan
Write-Host ""
Write-Host "  2. Test SignalR real-time updates:" -ForegroundColor White
Write-Host "     Open test-signalr.html in your browser" -ForegroundColor Cyan
Write-Host ""
Write-Host "  3. Create more test data using Swagger UI" -ForegroundColor White
Write-Host ""

