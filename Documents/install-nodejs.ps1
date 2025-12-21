# Node.js Installation Script
# This script will download and install Node.js LTS version

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Node.js Installation Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if already installed
try {
    $nodeVersion = node --version 2>&1
    if ($nodeVersion -match "v\d+") {
        Write-Host "✅ Node.js is already installed: $nodeVersion" -ForegroundColor Green
        Write-Host ""
        $npmVersion = npm --version 2>&1
        Write-Host "✅ npm version: $npmVersion" -ForegroundColor Green
        Write-Host ""
        Write-Host "Node.js is ready to use!" -ForegroundColor Green
        exit 0
    }
} catch {
    Write-Host "Node.js not found. Proceeding with installation..." -ForegroundColor Yellow
    Write-Host ""
}

# Try Winget first
if (Get-Command winget -ErrorAction SilentlyContinue) {
    Write-Host "Using Winget to install Node.js LTS..." -ForegroundColor Yellow
    Write-Host ""
    
    try {
        winget install OpenJS.NodeJS.LTS --accept-package-agreements --accept-source-agreements
        
        Write-Host ""
        Write-Host "✅ Installation completed via Winget!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Please close and reopen PowerShell, then verify with:" -ForegroundColor Yellow
        Write-Host "  node --version" -ForegroundColor Cyan
        Write-Host "  npm --version" -ForegroundColor Cyan
        exit 0
    } catch {
        Write-Host "Winget installation failed. Trying alternative method..." -ForegroundColor Yellow
        Write-Host ""
    }
}

# Try Chocolatey
if (Get-Command choco -ErrorAction SilentlyContinue) {
    Write-Host "Using Chocolatey to install Node.js LTS..." -ForegroundColor Yellow
    Write-Host "Note: This requires administrator privileges" -ForegroundColor Cyan
    Write-Host ""
    
    $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    
    if ($isAdmin) {
        try {
            choco install nodejs-lts -y
            Write-Host ""
            Write-Host "✅ Installation completed via Chocolatey!" -ForegroundColor Green
            Write-Host ""
            Write-Host "Please close and reopen PowerShell, then verify with:" -ForegroundColor Yellow
            Write-Host "  node --version" -ForegroundColor Cyan
            Write-Host "  npm --version" -ForegroundColor Cyan
            exit 0
        } catch {
            Write-Host "Chocolatey installation failed." -ForegroundColor Red
        }
    } else {
        Write-Host "Administrator privileges required for Chocolatey." -ForegroundColor Yellow
        Write-Host "Right-click PowerShell and select 'Run as Administrator', then run this script again." -ForegroundColor Cyan
        Write-Host ""
    }
}

# Manual download option
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "  Manual Installation Required" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "Please install Node.js manually:" -ForegroundColor White
Write-Host ""
Write-Host "1. Open your browser" -ForegroundColor Cyan
Write-Host "2. Go to: https://nodejs.org/" -ForegroundColor Cyan
Write-Host "3. Download the LTS version (recommended)" -ForegroundColor White
Write-Host "4. Run the installer (.msi file)" -ForegroundColor White
Write-Host "5. Make sure 'Add to PATH' is checked during installation" -ForegroundColor Yellow
Write-Host "6. Complete the installation" -ForegroundColor White
Write-Host "7. Close and reopen PowerShell" -ForegroundColor Yellow
Write-Host "8. Verify with: node --version" -ForegroundColor Cyan
Write-Host ""
Write-Host "After installation, you can run:" -ForegroundColor Yellow
Write-Host "  cd iot-monitoring-frontend" -ForegroundColor Cyan
Write-Host "  npm install" -ForegroundColor Cyan
Write-Host "  npm start" -ForegroundColor Cyan
Write-Host ""

