#Requires -RunAsAdministrator
# Right-click -> Run with PowerShell (as Administrator)

$ErrorActionPreference = "Stop"

function Add-RuleIfMissing {
    param([string]$DisplayName, [hashtable]$Params)
    if (Get-NetFirewallRule -DisplayName $DisplayName -ErrorAction SilentlyContinue) {
        Write-Host "Already exists: $DisplayName"
        return
    }
    New-NetFirewallRule @Params | Out-Null
    Write-Host "Created: $DisplayName"
}

# Allow on Private + Public (home Wi-Fi is sometimes "Public" on Windows)
Add-RuleIfMissing "Mosquitto MQTT 1883" @{
    DisplayName = "Mosquitto MQTT 1883"
    Direction   = "Inbound"
    Protocol    = "TCP"
    LocalPort   = 1883
    Action      = "Allow"
    Profile     = "Any"
}

$mosquittoExe = "C:\Program Files\mosquitto\mosquitto.exe"
if (Test-Path $mosquittoExe) {
    Add-RuleIfMissing "Mosquitto Broker" @{
        DisplayName = "Mosquitto Broker"
        Direction   = "Inbound"
        Program     = $mosquittoExe
        Action      = "Allow"
        Profile     = "Any"
    }
}

Write-Host ""
Get-NetFirewallRule -DisplayName "Mosquitto MQTT 1883", "Mosquitto Broker" -ErrorAction SilentlyContinue |
    Format-Table DisplayName, Enabled, Direction, Action, Profile -AutoSize

Write-Host "Done. Reset Arduino — Serial should show 'TCP probe OK' then 'MQTT connected'."
