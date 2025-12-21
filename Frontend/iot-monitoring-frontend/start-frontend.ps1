# Frontend Startup Script
# This script will check Node.js, install dependencies, and start the frontend

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  IoT Monitoring Frontend Startup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if Node.js is installed
Write-Host "Checking Node.js installation..." -ForegroundColor Yellow
try {
    $nodeVersion = node --version
    $npmVersion = npm --version
    Write-Host "✅ Node.js found: $nodeVersion" -ForegroundColor Green
    Write-Host "✅ npm found: $npmVersion" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "❌ Node.js is not installed!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install Node.js first:" -ForegroundColor Yellow
    Write-Host "  1. Go to: https://nodejs.org/" -ForegroundColor White
    Write-Host "  2. Download the LTS version" -ForegroundColor White
    Write-Host "  3. Run the installer" -ForegroundColor White
    Write-Host "  4. Restart PowerShell and try again" -ForegroundColor White
    Write-Host ""
    exit 1
}

# Check if node_modules exists
if (-not (Test-Path "node_modules")) {
    Write-Host "Installing dependencies..." -ForegroundColor Yellow
    Write-Host "This may take a few minutes..." -ForegroundColor Cyan
    Write-Host ""
    
    npm install
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to install dependencies" -ForegroundColor Red
        exit 1
    }
    
    Write-Host ""
    Write-Host "✅ Dependencies installed successfully!" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "✅ Dependencies already installed" -ForegroundColor Green
    Write-Host ""
}

# Check if backend is running
Write-Host "Checking backend API..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5286/swagger" -Method Get -TimeoutSec 2 -ErrorAction Stop
    Write-Host "✅ Backend API is running" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "⚠️  Backend API not detected at http://localhost:5286" -ForegroundColor Yellow
    Write-Host "   Make sure the backend is running before using the frontend" -ForegroundColor Yellow
    Write-Host ""
}

# Start the frontend
Write-Host "Starting frontend development server..." -ForegroundColor Yellow
Write-Host "The app will open at http://localhost:3000" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Yellow
Write-Host ""

npm start

