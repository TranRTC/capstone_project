# Quick cloud pipeline check (HiveMQ -> Azure API -> dashboard)
$api = "https://capstoneiotdashboard-gudbg0amfxeehae5.westus-01.azurewebsites.net/api/v1"

Write-Host "=== MQTT ingest (Azure API subscriber) ===" -ForegroundColor Cyan
try {
    $mqtt = Invoke-RestMethod -Uri "$api/health/mqtt" -TimeoutSec 30
    $d = $mqtt.data
    Write-Host "Broker: $($d.host):$($d.port) TLS=$($d.tlsEnabled)"
    Write-Host "Subscriber connected: $($d.subscriberConnected)  subscribed: $($d.subscriberSubscribed)"
    Write-Host "Last message received (UTC): $($d.lastMessageReceivedAtUtc)"
    Write-Host "Messages received: $($d.metrics.totalMessagesReceived)"
    Write-Host "Readings saved:      $($d.metrics.sensorReadingsPersisted)"
    Write-Host "Save errors:         $($d.metrics.sensorReadingPersistErrors)"
    if ($d.metrics.totalMessagesReceived -eq 0) {
        Write-Host ""
        Write-Host "PROBLEM: Azure has never received MQTT from HiveMQ." -ForegroundColor Yellow
        Write-Host "  - Run: python simulator-cloud.py --count 3"
        Write-Host "  - Then run this script again. If still 0, broker/credentials issue."
        Write-Host "  - If simulator works but Arduino does not, check Serial Monitor (TLS/pub OK)."
    }
    elseif ($d.metrics.sensorReadingPersistErrors -gt 0) {
        Write-Host ""
        Write-Host "PROBLEM: Messages arrive but DB save fails (wrong device/sensor IDs in cloud DB)." -ForegroundColor Yellow
    }
}
catch {
    Write-Host "Failed to call $api/health/mqtt : $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Optional: publish test from PC ===" -ForegroundColor Cyan
Write-Host '  cd "Sensor Testing"'
Write-Host "  python simulator-cloud.py --count 5 --interval 1"
Write-Host ""
Write-Host "Cloud dashboard: open device 1, sensors 1 and 2, chart mode Live." -ForegroundColor Cyan
