# Test script to verify backend API connection
Write-Host "Testing Backend API Connection..." -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:5000/api/v1"

# Test 1: Health check - Get devices endpoint
Write-Host "Test 1: Testing GET /devices endpoint..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/devices" -Method Get -ContentType "application/json" -ErrorAction Stop
    Write-Host "✓ SUCCESS: Backend is accessible!" -ForegroundColor Green
    Write-Host "  Response: $($response | ConvertTo-Json -Depth 2)" -ForegroundColor Gray
} catch {
    Write-Host "✗ FAILED: Cannot connect to backend" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Make sure the backend is running:" -ForegroundColor Yellow
    Write-Host "  cd Backend" -ForegroundColor White
    Write-Host "  dotnet run --project IoTMonitoringSystem.API/IoTMonitoringSystem.API.csproj" -ForegroundColor White
    exit 1
}

Write-Host ""

# Test 2: Check SignalR Hub endpoint
Write-Host "Test 2: Testing SignalR Hub endpoint..." -ForegroundColor Yellow
try {
    $hubUrl = "http://localhost:5000/monitoringhub"
    $response = Invoke-WebRequest -Uri $hubUrl -Method Get -ErrorAction Stop
    Write-Host "✓ SUCCESS: SignalR Hub is accessible!" -ForegroundColor Green
} catch {
    Write-Host "⚠ WARNING: SignalR Hub endpoint check failed (this is normal for SignalR)" -ForegroundColor Yellow
    Write-Host "  SignalR uses WebSocket protocol, not HTTP GET" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Backend connection test completed!" -ForegroundColor Cyan
Write-Host ""
Write-Host "Frontend Configuration:" -ForegroundColor Cyan
Write-Host "  API Base URL: $baseUrl" -ForegroundColor White
Write-Host "  SignalR Hub: http://localhost:5000/monitoringhub" -ForegroundColor White
Write-Host ""
Write-Host "If all tests passed, your frontend should be able to connect to the backend!" -ForegroundColor Green

