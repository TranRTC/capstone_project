# Frontend Diagnostic Script

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Frontend Diagnostic" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check Node.js
Write-Host "1. Checking Node.js..." -ForegroundColor Yellow
try {
    $nodeVersion = node --version
    Write-Host "   ✅ Node.js: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Node.js not found" -ForegroundColor Red
    exit 1
}

# Check npm
Write-Host "2. Checking npm..." -ForegroundColor Yellow
try {
    $npmVersion = npm --version
    Write-Host "   ✅ npm: $npmVersion" -ForegroundColor Green
} catch {
    Write-Host "   ❌ npm not found" -ForegroundColor Red
    exit 1
}

# Check if node_modules exists
Write-Host "3. Checking dependencies..." -ForegroundColor Yellow
if (Test-Path "node_modules") {
    Write-Host "   ✅ node_modules exists" -ForegroundColor Green
} else {
    Write-Host "   ❌ node_modules not found. Run: npm install --legacy-peer-deps" -ForegroundColor Red
    exit 1
}

# Check key files
Write-Host "4. Checking key files..." -ForegroundColor Yellow
$requiredFiles = @(
    "src\index.tsx",
    "src\App.tsx",
    "public\index.html",
    "package.json"
)

$allExist = $true
foreach ($file in $requiredFiles) {
    if (Test-Path $file) {
        Write-Host "   ✅ $file" -ForegroundColor Green
    } else {
        Write-Host "   ❌ $file - MISSING!" -ForegroundColor Red
        $allExist = $false
    }
}

if (-not $allExist) {
    Write-Host "`n❌ Missing required files!" -ForegroundColor Red
    exit 1
}

# Check if server is running
Write-Host "5. Checking if server is running..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:3000" -Method Get -TimeoutSec 2 -ErrorAction Stop
    Write-Host "   ✅ Server is responding (Status: $($response.StatusCode))" -ForegroundColor Green
    
    if ($response.Content -match "root") {
        Write-Host "   ✅ HTML contains root element" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  HTML might be incomplete" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ❌ Server not responding: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "`n   Make sure 'npm start' is running!" -ForegroundColor Yellow
}

# Check for compilation errors
Write-Host "6. Testing compilation..." -ForegroundColor Yellow
try {
    $buildOutput = npm run build 2>&1 | Out-String
    if ($buildOutput -match "Compiled successfully" -or $buildOutput -match "build/static") {
        Write-Host "   ✅ Build successful" -ForegroundColor Green
    } elseif ($buildOutput -match "error" -or $buildOutput -match "Error") {
        Write-Host "   ❌ Build has errors:" -ForegroundColor Red
        $buildOutput -split "`n" | Where-Object { $_ -match "error|Error" } | ForEach-Object {
            Write-Host "      $_" -ForegroundColor Red
        }
    } else {
        Write-Host "   ⚠️  Build status unclear" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ⚠️  Could not test build" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Diagnostic Complete" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Check browser console (F12) for JavaScript errors" -ForegroundColor White
Write-Host "  2. Check terminal where 'npm start' is running" -ForegroundColor White
Write-Host "  3. Try hard refresh: Ctrl+F5" -ForegroundColor White
Write-Host "  4. Check if backend is running: http://localhost:5286/swagger" -ForegroundColor White
Write-Host ""

