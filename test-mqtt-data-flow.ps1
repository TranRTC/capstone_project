param(
    [string]$ApiBaseUrl = "http://localhost:5000/api/v1",
    [int]$DeviceId = 1,
    [int]$SensorId = 1,
    [int]$WaitSeconds = 4
)

$ErrorActionPreference = "Stop"

function Write-Step([string]$message) {
    Write-Host ""
    Write-Host $message -ForegroundColor Yellow
}

function Exit-Fail([string]$message) {
    Write-Host "[FAIL] $message" -ForegroundColor Red
    exit 1
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Pre-Deploy MQTT Smoke Test" -ForegroundColor Cyan
Write-Host "API + MQTT + DB ingest verification" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Config: ApiBaseUrl=$ApiBaseUrl, DeviceId=$DeviceId, SensorId=$SensorId" -ForegroundColor Gray

Write-Step "Step 1/6: API health"
$health = Invoke-RestMethod -Uri "$ApiBaseUrl/health" -Method Get
if (-not $health.success -or $health.data.status -ne "healthy") {
    Exit-Fail "API health endpoint did not report healthy."
}
Write-Host "[OK] API healthy at $ApiBaseUrl" -ForegroundColor Green

Write-Step "Step 2/6: MQTT broker + subscriber health"
$mqttHealth = Invoke-RestMethod -Uri "$ApiBaseUrl/health/mqtt" -Method Get
if (-not $mqttHealth.success -or $mqttHealth.data.status -ne "ready") {
    Exit-Fail "MQTT health endpoint is not ready (status=$($mqttHealth.data.status))."
}

if ($null -ne $mqttHealth.data.subscriberConnected -and -not $mqttHealth.data.subscriberConnected) {
    Exit-Fail "MQTT subscriber is not connected in backend runtime state."
}
if ($null -ne $mqttHealth.data.subscriberSubscribed -and -not $mqttHealth.data.subscriberSubscribed) {
    Exit-Fail "MQTT subscriber is connected but not subscribed."
}
Write-Host "[OK] MQTT broker ready; backend subscriber connected/subscribed." -ForegroundColor Green

Write-Step "Step 3/6: Verify target device/sensor exist"
$device = Invoke-RestMethod -Uri "$ApiBaseUrl/devices/$DeviceId" -Method Get
if (-not $device.success) {
    Exit-Fail "Device $DeviceId not found."
}
$sensors = Invoke-RestMethod -Uri "$ApiBaseUrl/sensors/devices/$DeviceId/sensors" -Method Get
if (-not $sensors.success) {
    Exit-Fail "Could not fetch sensors for device $DeviceId."
}
$targetSensor = $sensors.data | Where-Object { $_.sensorId -eq $SensorId } | Select-Object -First 1
if (-not $targetSensor) {
    Exit-Fail "Sensor $SensorId does not exist on device $DeviceId."
}
Write-Host "[OK] Device and sensor found." -ForegroundColor Green

Write-Step "Step 4/6: Baseline latest reading id"
$beforeReadings = Invoke-RestMethod -Uri "$ApiBaseUrl/sensorreadings?sensorId=$SensorId&pageNumber=1&pageSize=1" -Method Get
$beforeId = if ($beforeReadings.success -and $beforeReadings.data.items.Count -gt 0) { [int64]$beforeReadings.data.items[0].readingId } else { 0 }
Write-Host "Baseline ReadingId: $beforeId" -ForegroundColor Gray

Write-Step "Step 5/6: Publish one MQTT test value"
$pythonCheck = python --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Exit-Fail "Python is required for this smoke test (simulator)."
}
python -c "import paho.mqtt.client" 2>$null
if ($LASTEXITCODE -ne 0) {
    Exit-Fail "Python package 'paho-mqtt' is missing. Install with: pip install paho-mqtt"
}

$testValue = [Math]::Round((Get-Random -Minimum 2000 -Maximum 3000) / 100.0, 2)
Write-Host "Publishing value: $testValue" -ForegroundColor Cyan
python "test-mqtt-sensor-simulator.py" --device-id $DeviceId --sensor-id $SensorId --value $testValue --topic-format devices
if ($LASTEXITCODE -ne 0) {
    Exit-Fail "Simulator failed to publish test value."
}

Write-Host "Waiting $WaitSeconds second(s) for ingest..." -ForegroundColor Gray
Start-Sleep -Seconds $WaitSeconds

Write-Step "Step 6/6: Verify new reading persisted"
$afterReadings = Invoke-RestMethod -Uri "$ApiBaseUrl/sensorreadings?sensorId=$SensorId&pageNumber=1&pageSize=3" -Method Get
if (-not $afterReadings.success -or $afterReadings.data.items.Count -eq 0) {
    Exit-Fail "No readings returned for sensor $SensorId after publish."
}

$latest = $afterReadings.data.items[0]
$latestId = [int64]$latest.readingId
if ($latestId -le $beforeId) {
    Exit-Fail "No new reading persisted. Latest ReadingId ($latestId) <= baseline ($beforeId)."
}

$valueDiff = [Math]::Abs([decimal]$latest.value - [decimal]$testValue)
if ($valueDiff -gt 0.02) {
    Write-Host "[WARN] New row detected but value differs from sent value (sent=$testValue, got=$($latest.value))." -ForegroundColor Yellow
} else {
    Write-Host "[OK] New reading value matches sent test value." -ForegroundColor Green
}

Write-Host ""
Write-Host "[PASS] Smoke test succeeded." -ForegroundColor Green
Write-Host "Latest reading -> id=$($latest.readingId), device=$($latest.deviceId), sensor=$($latest.sensorId), value=$($latest.value), ts=$($latest.timestamp)" -ForegroundColor White


