# Test MQTT Data Flow: Sensor -> Broker -> Server
# This script tests the complete data flow from sensor to broker to server

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MQTT Data Flow Test" -ForegroundColor Cyan
Write-Host "Sensor -> Broker -> Server" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$API_BASE_URL = "http://localhost:5000/api/v1"
$MQTT_BROKER_HOST = "localhost"
$MQTT_BROKER_PORT = 1883

# Step 1: Check if MQTT broker is running
Write-Host "Step 1: Checking MQTT Broker..." -ForegroundColor Yellow
try {
    $tcpClient = New-Object System.Net.Sockets.TcpClient
    $tcpClient.Connect($MQTT_BROKER_HOST, $MQTT_BROKER_PORT)
    $tcpClient.Close()
    Write-Host "✓ MQTT Broker is accessible at $MQTT_BROKER_HOST`:$MQTT_BROKER_PORT" -ForegroundColor Green
} catch {
    Write-Host "✗ MQTT Broker is NOT accessible at $MQTT_BROKER_HOST`:$MQTT_BROKER_PORT" -ForegroundColor Red
    Write-Host "  Please start an MQTT broker (e.g., Mosquitto) on port 1883" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  To install Mosquitto on Windows:" -ForegroundColor Cyan
    Write-Host "  1. Download from: https://mosquitto.org/download/" -ForegroundColor White
    Write-Host "  2. Or use: choco install mosquitto" -ForegroundColor White
    Write-Host "  3. Start service: net start mosquitto" -ForegroundColor White
    exit 1
}
Write-Host ""

# Step 2: Check if Backend API is running
Write-Host "Step 2: Checking Backend API..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$API_BASE_URL/devices" -Method Get -UseBasicParsing -ErrorAction Stop
    Write-Host "✓ Backend API is accessible at $API_BASE_URL" -ForegroundColor Green
} catch {
    Write-Host "✗ Backend API is NOT accessible at $API_BASE_URL" -ForegroundColor Red
    Write-Host "  Please start the backend API first" -ForegroundColor Yellow
    Write-Host "  Run: cd Backend/IoTMonitoringSystem.API && dotnet run" -ForegroundColor White
    exit 1
}
Write-Host ""

# Step 3: Get or create a test device and sensor
Write-Host "Step 3: Setting up test device and sensor..." -ForegroundColor Yellow
$deviceId = $null
$sensorId = $null

try {
    # Get existing devices
    $devicesResponse = Invoke-RestMethod -Uri "$API_BASE_URL/devices" -Method Get
    if ($devicesResponse.success -and $devicesResponse.data.Count -gt 0) {
        $deviceId = $devicesResponse.data[0].deviceId
        Write-Host "  Using existing device ID: $deviceId" -ForegroundColor Gray
        
        # Get sensors for this device
        $sensorsResponse = Invoke-RestMethod -Uri "$API_BASE_URL/sensors/devices/$deviceId/sensors" -Method Get
        if ($sensorsResponse.success -and $sensorsResponse.data.Count -gt 0) {
            $sensorId = $sensorsResponse.data[0].sensorId
            Write-Host "  Using existing sensor ID: $sensorId" -ForegroundColor Gray
        }
    }
} catch {
    Write-Host "  Warning: Could not fetch existing devices/sensors" -ForegroundColor Yellow
}

if (-not $deviceId) {
    Write-Host "  Creating test device..." -ForegroundColor Gray
    try {
        $newDevice = @{
            deviceName = "Test Device - MQTT"
            deviceType = "Sensor Hub"
            location = "Test Lab"
            status = "Active"
        } | ConvertTo-Json
        
        $deviceResponse = Invoke-RestMethod -Uri "$API_BASE_URL/devices" -Method Post -Body $newDevice -ContentType "application/json"
        if ($deviceResponse.success) {
            $deviceId = $deviceResponse.data.deviceId
            Write-Host "  ✓ Created device ID: $deviceId" -ForegroundColor Green
        }
    } catch {
        Write-Host "  ✗ Failed to create device: $_" -ForegroundColor Red
        exit 1
    }
}

if (-not $sensorId -and $deviceId) {
    Write-Host "  Creating test sensor..." -ForegroundColor Gray
    try {
        $newSensor = @{
            sensorName = "Test Sensor - MQTT"
            sensorType = "Temperature"
            unit = "Celsius"
            status = "Active"
        } | ConvertTo-Json
        
        $sensorResponse = Invoke-RestMethod -Uri "$API_BASE_URL/sensors/devices/$deviceId/sensors" -Method Post -Body $newSensor -ContentType "application/json"
        if ($sensorResponse.success) {
            $sensorId = $sensorResponse.data.sensorId
            Write-Host "  ✓ Created sensor ID: $sensorId" -ForegroundColor Green
        }
    } catch {
        Write-Host "  ✗ Failed to create sensor: $_" -ForegroundColor Red
        exit 1
    }
}

if (-not $deviceId -or -not $sensorId) {
    Write-Host "✗ Could not get or create device/sensor" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Test setup complete: Device ID=$deviceId, Sensor ID=$sensorId" -ForegroundColor Green
Write-Host ""

# Step 4: Get initial reading count
Write-Host "Step 4: Getting initial reading count..." -ForegroundColor Yellow
try {
    $initialReadings = Invoke-RestMethod -Uri "$API_BASE_URL/sensorreadings?sensorId=$sensorId&pageSize=1000" -Method Get
    $initialCount = if ($initialReadings.success) { $initialReadings.data.items.Count } else { 0 }
    Write-Host "  Initial readings count: $initialCount" -ForegroundColor Gray
} catch {
    $initialCount = 0
    Write-Host "  Could not get initial count, assuming 0" -ForegroundColor Yellow
}
Write-Host ""

# Step 5: Check if Python and paho-mqtt are available
Write-Host "Step 5: Checking Python sensor simulator..." -ForegroundColor Yellow
$pythonAvailable = $false
try {
    $pythonVersion = python --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ Python found: $pythonVersion" -ForegroundColor Green
        $pythonAvailable = $true
        
        # Check for paho-mqtt
        $mqttCheck = python -c "import paho.mqtt.client; print('OK')" 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✓ paho-mqtt library is installed" -ForegroundColor Green
        } else {
            Write-Host "  ✗ paho-mqtt library is NOT installed" -ForegroundColor Red
            Write-Host "  Install it with: pip install paho-mqtt" -ForegroundColor Yellow
            $pythonAvailable = $false
        }
    } else {
        Write-Host "  ✗ Python is not available" -ForegroundColor Red
    }
} catch {
    Write-Host "  ✗ Python is not available" -ForegroundColor Red
}

if (-not $pythonAvailable) {
    Write-Host ""
    Write-Host "⚠ Python sensor simulator is not available" -ForegroundColor Yellow
    Write-Host "  You can manually test by:" -ForegroundColor Cyan
    Write-Host "  1. Installing Python and paho-mqtt: pip install paho-mqtt" -ForegroundColor White
    Write-Host "  2. Running: python test-mqtt-sensor-simulator.py --device-id $deviceId --sensor-id $sensorId --count 5" -ForegroundColor White
    Write-Host ""
    Write-Host "  Or use an MQTT client tool like MQTT.fx or mosquitto_pub" -ForegroundColor White
    Write-Host ""
    exit 0
}
Write-Host ""

# Step 6: Send test sensor readings via MQTT
Write-Host "Step 6: Sending test sensor readings via MQTT..." -ForegroundColor Yellow
Write-Host "  Device ID: $deviceId, Sensor ID: $sensorId" -ForegroundColor Gray
Write-Host ""

$testValue = [math]::Round((Get-Random -Minimum 20 -Maximum 80) + (Get-Random) / 10, 2)
Write-Host "  Sending test reading with value: $testValue" -ForegroundColor Cyan

try {
    python test-mqtt-sensor-simulator.py --device-id $deviceId --sensor-id $sensorId --value $testValue --topic-format devices
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ Test reading sent successfully" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Failed to send test reading" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "  ✗ Error sending test reading: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "  Waiting 3 seconds for server to process..." -ForegroundColor Gray
Start-Sleep -Seconds 3
Write-Host ""

# Step 7: Verify data was received and saved
Write-Host "Step 7: Verifying data was received by server..." -ForegroundColor Yellow
try {
    $readingsResponse = Invoke-RestMethod -Uri "$API_BASE_URL/sensorreadings?sensorId=$sensorId&pageSize=1000" -Method Get
    if ($readingsResponse.success) {
        $newCount = $readingsResponse.data.items.Count
        $newReadings = $newCount - $initialCount
        
        Write-Host "  Initial count: $initialCount" -ForegroundColor Gray
        Write-Host "  Current count: $newCount" -ForegroundColor Gray
        Write-Host "  New readings: $newReadings" -ForegroundColor Gray
        
        if ($newReadings -gt 0) {
            Write-Host "  ✓ Data successfully received and saved!" -ForegroundColor Green
            
            # Get the latest reading
            $latestReading = $readingsResponse.data.items | Sort-Object -Property timestamp -Descending | Select-Object -First 1
            if ($latestReading) {
                Write-Host ""
                Write-Host "  Latest Reading Details:" -ForegroundColor Cyan
                Write-Host "    Reading ID: $($latestReading.readingId)" -ForegroundColor White
                Write-Host "    Device ID: $($latestReading.deviceId)" -ForegroundColor White
                Write-Host "    Sensor ID: $($latestReading.sensorId)" -ForegroundColor White
                Write-Host "    Value: $($latestReading.value)" -ForegroundColor White
                Write-Host "    Timestamp: $($latestReading.timestamp)" -ForegroundColor White
                Write-Host "    Status: $($latestReading.status)" -ForegroundColor White
                
                # Verify the value matches (within tolerance)
                $valueDiff = [math]::Abs($latestReading.value - $testValue)
                if ($valueDiff -lt 0.01) {
                    Write-Host ""
                    Write-Host "  ✓ Value matches sent value ($testValue)" -ForegroundColor Green
                } else {
                    Write-Host ""
                    Write-Host "  ⚠ Value mismatch: sent=$testValue, received=$($latestReading.value)" -ForegroundColor Yellow
                }
            }
        } else {
            Write-Host "  ✗ No new readings found. Data may not have been processed." -ForegroundColor Red
            Write-Host "  Check backend logs for MQTT connection and processing errors." -ForegroundColor Yellow
        }
    } else {
        Write-Host "  ✗ Failed to retrieve readings: $($readingsResponse.message)" -ForegroundColor Red
    }
} catch {
    Write-Host "  ✗ Error verifying data: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test Complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "To send more test readings, run:" -ForegroundColor Cyan
Write-Host "  python test-mqtt-sensor-simulator.py --device-id $deviceId --sensor-id $sensorId --count 10" -ForegroundColor White
Write-Host ""

