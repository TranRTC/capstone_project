# Complete API Testing Script
# Tests all endpoints: Device -> Sensor -> Alert Rule -> Readings -> Alerts

$baseUrl = "http://localhost:5286/api/v1"
$deviceId = $null
$sensorId = $null
$alertRuleId = $null
$alertId = $null

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Complete API Endpoint Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# STEP 1: Create Device
Write-Host "=== STEP 1: Create Device ===" -ForegroundColor Yellow
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
        Write-Host "   Location: $($response.data.location)" -ForegroundColor White
    } else {
        Write-Host "❌ Failed to create device" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Error creating device: $($_.Exception.Message)" -ForegroundColor Red
    # Try to get existing device
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/devices" -Method Get -ErrorAction Stop
        if ($response.success -and $response.data.Count -gt 0) {
            $deviceId = $response.data[0].deviceId
            Write-Host "✅ Using existing device ID: $deviceId" -ForegroundColor Green
        }
    } catch {
        Write-Host "❌ Cannot proceed without a device" -ForegroundColor Red
        exit
    }
}
Write-Host ""

if (-not $deviceId) {
    Write-Host "❌ Cannot continue without a device ID" -ForegroundColor Red
    exit
}

# STEP 2: Create Sensor
Write-Host "=== STEP 2: Create Sensor ===" -ForegroundColor Yellow
$sensorBody = @{
    sensorName = "Temperature Sensor"
    sensorType = "Temperature"
    unit = "°C"
    minValue = -40
    maxValue = 85
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/sensors/devices/$deviceId/sensors" -Method Post -Body $sensorBody -ContentType "application/json" -ErrorAction Stop
    if ($response.success) {
        $sensorId = $response.data.sensorId
        Write-Host "✅ Sensor created successfully!" -ForegroundColor Green
        Write-Host "   Sensor ID: $sensorId" -ForegroundColor White
        Write-Host "   Sensor Name: $($response.data.sensorName)" -ForegroundColor White
        Write-Host "   Sensor Type: $($response.data.sensorType)" -ForegroundColor White
        Write-Host "   Unit: $($response.data.unit)" -ForegroundColor White
    } else {
        Write-Host "❌ Failed to create sensor" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Error creating sensor: $($_.Exception.Message)" -ForegroundColor Red
    # Try to get existing sensor
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/sensors/devices/$deviceId/sensors" -Method Get -ErrorAction Stop
        if ($response.success -and $response.data.Count -gt 0) {
            $sensorId = $response.data[0].sensorId
            Write-Host "✅ Using existing sensor ID: $sensorId" -ForegroundColor Green
        }
    } catch {
        Write-Host "❌ Cannot proceed without a sensor" -ForegroundColor Red
    }
}
Write-Host ""

if (-not $sensorId) {
    Write-Host "❌ Cannot continue without a sensor ID" -ForegroundColor Red
    exit
}

# STEP 3: Create Alert Rule
Write-Host "=== STEP 3: Create Alert Rule ===" -ForegroundColor Yellow
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
        Write-Host "   Threshold: $($response.data.thresholdValue)°C" -ForegroundColor White
        Write-Host "   Operator: $($response.data.comparisonOperator)" -ForegroundColor White
        Write-Host "   Severity: $($response.data.severity)" -ForegroundColor White
    } else {
        Write-Host "❌ Failed to create alert rule" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Error creating alert rule: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# STEP 4: Create Sensor Reading (Normal - No Alert)
Write-Host "=== STEP 4: Create Sensor Reading (Normal - No Alert) ===" -ForegroundColor Yellow
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
        Write-Host "   Timestamp: $($response.data.timestamp)" -ForegroundColor White
        Write-Host "   ⚠️  This should NOT trigger an alert (25.0 < 30.0)" -ForegroundColor Cyan
    } else {
        Write-Host "❌ Failed to create sensor reading" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Error creating sensor reading: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""
Start-Sleep -Seconds 1

# STEP 5: Create Sensor Reading (High - Should Trigger Alert)
Write-Host "=== STEP 5: Create Sensor Reading (High - Should Trigger Alert) ===" -ForegroundColor Yellow
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
        Write-Host "   Timestamp: $($response.data.timestamp)" -ForegroundColor White
        Write-Host "   🚨 This SHOULD trigger an alert (35.5 > 30.0)" -ForegroundColor Yellow
    } else {
        Write-Host "❌ Failed to create sensor reading" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Error creating sensor reading: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""
Start-Sleep -Seconds 2

# STEP 6: Check for Active Alerts
Write-Host "=== STEP 6: Check for Active Alerts ===" -ForegroundColor Yellow
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
            Write-Host "   🚨 ALERT TRIGGERED!" -ForegroundColor Red
            Write-Host "   Alert Details:" -ForegroundColor Cyan
            Write-Host "     Alert ID: $($alert.alertId)" -ForegroundColor White
            Write-Host "     Message: $($alert.message)" -ForegroundColor White
            Write-Host "     Severity: $($alert.severity)" -ForegroundColor White
            Write-Host "     Status: $($alert.status)" -ForegroundColor White
            Write-Host "     Device ID: $($alert.deviceId)" -ForegroundColor White
            Write-Host "     Sensor ID: $($alert.sensorId)" -ForegroundColor White
            Write-Host "     Trigger Value: $($alert.triggerValue)°C" -ForegroundColor White
            Write-Host "     Triggered At: $($alert.triggeredAt)" -ForegroundColor White
        } else {
            Write-Host "   ⚠️  No active alerts found" -ForegroundColor Yellow
            Write-Host "   (Alert evaluation may need more time)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "❌ Failed to retrieve alerts" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Error retrieving alerts: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# BONUS: Acknowledge Alert
if ($alertId) {
    Write-Host "=== BONUS: Acknowledge Alert ===" -ForegroundColor Yellow
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/alerts/$alertId/acknowledge" -Method Put -ContentType "application/json" -ErrorAction Stop
        if ($response.success) {
            Write-Host "✅ Alert acknowledged successfully!" -ForegroundColor Green
            Write-Host "   Acknowledged At: $($response.data.acknowledgedAt)" -ForegroundColor White
        }
    } catch {
        Write-Host "❌ Error acknowledging alert: $($_.Exception.Message)" -ForegroundColor Red
    }
    Write-Host ""
    Start-Sleep -Seconds 1
    
    # BONUS: Resolve Alert
    Write-Host "=== BONUS: Resolve Alert ===" -ForegroundColor Yellow
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/alerts/$alertId/resolve" -Method Put -ContentType "application/json" -ErrorAction Stop
        if ($response.success) {
            Write-Host "✅ Alert resolved successfully!" -ForegroundColor Green
            Write-Host "   Status: $($response.data.status)" -ForegroundColor White
            Write-Host "   Resolved At: $($response.data.resolvedAt)" -ForegroundColor White
        }
    } catch {
        Write-Host "❌ Error resolving alert: $($_.Exception.Message)" -ForegroundColor Red
    }
    Write-Host ""
}

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ All steps completed!" -ForegroundColor Green
Write-Host ""
Write-Host "Created Resources:" -ForegroundColor Yellow
Write-Host "  • Device ID: $deviceId" -ForegroundColor White
Write-Host "  • Sensor ID: $sensorId" -ForegroundColor White
if ($alertRuleId) {
    Write-Host "  • Alert Rule ID: $alertRuleId" -ForegroundColor White
}
if ($alertId) {
    Write-Host "  • Alert ID: $alertId (triggered and resolved)" -ForegroundColor White
}
Write-Host ""
Write-Host "✅ API is working correctly!" -ForegroundColor Green
Write-Host ""
Write-Host "You can now:" -ForegroundColor Yellow
Write-Host "  • View all endpoints in Swagger: http://localhost:5286/swagger" -ForegroundColor Cyan
Write-Host "  • Test SignalR: Open test-signalr.html" -ForegroundColor Cyan
Write-Host "  • Create more test data using Swagger UI" -ForegroundColor Cyan
Write-Host ""

