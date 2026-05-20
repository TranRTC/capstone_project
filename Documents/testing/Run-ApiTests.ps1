# Automated API test script for IoT Monitoring System
param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$Username = "admin",
    [string]$Password = "Admin@123"
)

$ErrorActionPreference = "Stop"
$api = "$BaseUrl/api/v1"
$results = [System.Collections.Generic.List[object]]::new()

function Add-Result($Phase, $Id, $Name, $Pass, $Detail) {
    $results.Add([PSCustomObject]@{
        Phase = $Phase; Id = $Id; Name = $Name; Pass = $Pass; Detail = $Detail
    }) | Out-Null
}

function Get-ApiData($jsonText) {
    $o = $jsonText | ConvertFrom-Json
    if ($null -ne $o.data) { return $o.data }
    return $o
}

function Try-Request {
    param($Method, $Uri, $Headers = @{}, $Body = $null, [int[]]$Ok = @(200))
    try {
        $params = @{ Method = $Method; Uri = $Uri; Headers = $Headers; UseBasicParsing = $true }
        if ($Body) { $params.Body = $Body; $params.ContentType = "application/json" }
        $r = Invoke-WebRequest @params
        return @{ Ok = ($r.StatusCode -in $Ok); Status = $r.StatusCode; Content = $r.Content }
    }
    catch {
        $status = $null
        if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode }
        return @{ Ok = ($status -in $Ok); Status = $status; Content = $_.Exception.Message }
    }
}

Write-Host "API tests against $api" -ForegroundColor Cyan

# Phase 1 — Health
$r = Try-Request GET "$api/health"
Add-Result "1" "1.1" "GET /health" $r.Ok "Status $($r.Status)"

$r = Try-Request GET "$api/health/mqtt"
Add-Result "1" "1.2" "GET /health/mqtt" $r.Ok "Status $($r.Status)"

$r = Try-Request GET "$BaseUrl/swagger/index.html"
Add-Result "1" "1.3" "Swagger UI" $r.Ok "Status $($r.Status)"

# Phase 2 — Auth
$r = Try-Request GET "$api/devices" -Ok @(401)
Add-Result "2" "2.2" "GET /devices no token" $r.Ok "Status $($r.Status)"

$loginBody = (@{ username = $Username; password = $Password } | ConvertTo-Json)
$r = Try-Request POST "$api/auth/login" -Body $loginBody -Ok @(200)
$token = $null
if ($r.Ok) {
    $login = $r.Content | ConvertFrom-Json
    $token = $login.token
}
Add-Result "2" "2.1" "POST /auth/login" ($r.Ok -and $token) $(if ($token) { "token received" } else { "no token" })

$badLogin = (@{ username = $Username; password = "wrong" } | ConvertTo-Json)
$r = Try-Request POST "$api/auth/login" -Body $badLogin -Ok @(401)
Add-Result "2" "2.4" "POST /auth/login bad password" $r.Ok "Status $($r.Status)"

if (-not $token) {
    Write-Host "Cannot continue without token." -ForegroundColor Red
    $results | Format-Table
    exit 1
}

$auth = @{ Authorization = "Bearer $token" }
$suffix = (Get-Date -Format "yyyyMMddHHmmss")

# Phase 3 — Devices
$deviceBody = (@{
    deviceName = "API Test Device $suffix"
    deviceType = "Temperature"
    location = "Test Lab"
    facilityType = "Laboratory"
    edgeDeviceType = "ESP32"
    edgeDeviceId = "TEST-$suffix"
    description = "Automated test"
} | ConvertTo-Json)

$r = Try-Request POST "$api/devices" -Headers $auth -Body $deviceBody -Ok @(200, 201)
$deviceId = $null
if ($r.Ok) {
    $deviceId = (Get-ApiData $r.Content).deviceId
}
Add-Result "3" "3.1.1" "POST /devices" ($r.Ok -and $deviceId) "deviceId=$deviceId"

$r = Try-Request GET "$api/devices" -Headers $auth
Add-Result "3" "3.1.2" "GET /devices" $r.Ok "Status $($r.Status)"

if ($deviceId) {
    $r = Try-Request GET "$api/devices/$deviceId" -Headers $auth
    Add-Result "3" "3.1.3" "GET /devices/{id}" $r.Ok "Status $($r.Status)"

    $r = Try-Request GET "$api/devices/$deviceId/status" -Headers $auth
    Add-Result "3" "3.1.5" "GET /devices/{id}/status" $r.Ok "Status $($r.Status)"

    $sensorBody = (@{
        sensorName = "Test Sensor"
        sensorType = "Temperature"
        unit = "C"
        minValue = 0
        maxValue = 100
        isActive = $true
    } | ConvertTo-Json)

    $r = Try-Request POST "$api/sensors/devices/$deviceId/sensors" -Headers $auth -Body $sensorBody -Ok @(200, 201)
    $sensorId = $null
    if ($r.Ok) { $sensorId = (Get-ApiData $r.Content).sensorId }
    Add-Result "3" "3.2.1" "POST sensor" ($r.Ok -and $sensorId) "sensorId=$sensorId"

    $r = Try-Request GET "$api/sensors/devices/$deviceId/sensors" -Headers $auth
    Add-Result "3" "3.2.2" "GET sensors by device" $r.Ok "Status $($r.Status)"

    if ($sensorId) {
        $readingBody = (@{
            deviceId = $deviceId
            sensorId = $sensorId
            value = 22.5
            timestamp = (Get-Date).ToUniversalTime().ToString("o")
        } | ConvertTo-Json)
        $r = Try-Request POST "$api/sensorreadings" -Headers $auth -Body $readingBody -Ok @(200, 201)
        Add-Result "3" "3.3.1" "POST sensorreading" $r.Ok "Status $($r.Status)"

        $r = Try-Request GET "$api/sensorreadings/devices/$deviceId/readings" -Headers $auth
        Add-Result "3" "3.3.2" "GET readings by device" $r.Ok "Status $($r.Status)"

        $ruleBody = (@{
            deviceId = $deviceId
            sensorId = $sensorId
            ruleName = "High temp $suffix"
            ruleType = "threshold"
            condition = "Temperature above threshold"
            comparisonOperator = ">"
            thresholdValue = 20
            severity = "Warning"
            isEnabled = $true
        } | ConvertTo-Json)
        $r = Try-Request POST "$api/alertrules" -Headers $auth -Body $ruleBody -Ok @(200, 201)
        Add-Result "3" "3.4.1" "POST alertrule" $r.Ok "Status $($r.Status)"
    }
}

$r = Try-Request GET "$api/alerts/active" -Headers $auth
Add-Result "3" "3.4.2" "GET alerts/active" $r.Ok "Status $($r.Status)"

$r = Try-Request GET "$api/alertrules" -Headers $auth
Add-Result "3" "3.4.x" "GET alertrules" $r.Ok "Status $($r.Status)"

$r = Try-Request GET "$api/auth/users" -Headers $auth
Add-Result "3" "3.7.1" "GET /auth/users" $r.Ok "Status $($r.Status)"

$passed = ($results | Where-Object { $_.Pass }).Count
$total = $results.Count
Write-Host ""
Write-Host "Results: $passed / $total passed" -ForegroundColor $(if ($passed -eq $total) { "Green" } else { "Yellow" })
$results | Format-Table Phase, Id, Name, Pass, Detail -AutoSize

if ($passed -ne $total) { exit 1 }
