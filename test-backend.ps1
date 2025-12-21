# Quick Backend Test Script
Write-Host "Testing Backend Connection..." -ForegroundColor Cyan
Write-Host ""

$url = "http://localhost:5000/api/v1/devices"

try {
    Write-Host "Testing: $url" -ForegroundColor Yellow
    $response = Invoke-WebRequest -Uri $url -Method Get -UseBasicParsing -ErrorAction Stop
    
    Write-Host "✓ SUCCESS! Backend is accessible" -ForegroundColor Green
    Write-Host "Status Code: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Response Length: $($response.Content.Length) bytes" -ForegroundColor Green
    
    # Check for CORS headers
    if ($response.Headers['Access-Control-Allow-Origin']) {
        Write-Host "✓ CORS Headers Present: $($response.Headers['Access-Control-Allow-Origin'])" -ForegroundColor Green
    } else {
        Write-Host "⚠ WARNING: No CORS headers found" -ForegroundColor Yellow
    }
    
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "✗ FAILED" -ForegroundColor Red
    Write-Host "Status Code: $statusCode" -ForegroundColor Red
    
    if ($statusCode -eq 307) {
        Write-Host ""
        Write-Host "⚠ 307 REDIRECT DETECTED!" -ForegroundColor Red
        Write-Host "The backend is redirecting HTTP to HTTPS." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "SOLUTION:" -ForegroundColor Cyan
        Write-Host "1. Stop the backend (Ctrl+C)" -ForegroundColor White
        Write-Host "2. Restart with: dotnet run --project IoTMonitoringSystem.API/IoTMonitoringSystem.API.csproj --launch-profile http" -ForegroundColor White
        Write-Host "3. Make sure it shows: 'Now listening on: http://localhost:5000' (NOT https)" -ForegroundColor White
    } elseif ($statusCode -eq 0 -or $null -eq $statusCode) {
        Write-Host ""
        Write-Host "⚠ Backend is not running or not accessible" -ForegroundColor Red
        Write-Host "Make sure the backend is running on port 5000" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Test completed!" -ForegroundColor Cyan

