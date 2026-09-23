<#
  Registers two BloatBlocker scheduled tasks, both elevated:
  - "BloatBlocker Startup": launches the actual app (tray icon + dashboard) at
    every logon. Using a scheduled task instead of a registry Run key because
    Run-key entries can't reliably launch an app that requires admin - Windows
    either blocks it or needs a UAC prompt that can't appear that early in the
    login sequence, so it just fails silently. A scheduled task with
    RunLevel Highest launches elevated without a prompt.
  - "BloatBlocker Drift Check": runs "BloatBlocker.exe --check" at logon and
    daily - a silent, no-window status check (in case the app isn't left
    running until next login).

  Uses Register-ScheduledTask rather than schtasks.exe - schtasks silently
  mangled the quoted exe path during SystemPulse's install, causing the task
  to register but never actually launch the app.
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$InstallDir
)

$exePath = Join-Path $InstallDir "BloatBlocker.exe"

$principal = New-ScheduledTaskPrincipal -UserId "$env:USERDOMAIN\$env:USERNAME" `
    -LogonType Interactive -RunLevel Highest

$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries -StartWhenAvailable

# Startup task - launches the full app, no arguments
$startupAction = New-ScheduledTaskAction -Execute $exePath
$startupTrigger = New-ScheduledTaskTrigger -AtLogOn

Register-ScheduledTask -TaskName "BloatBlocker Startup" `
    -Action $startupAction `
    -Trigger $startupTrigger `
    -Principal $principal `
    -Settings $settings `
    -Force

# Drift-check task - headless "--check" mode, logon + daily
$checkAction = New-ScheduledTaskAction -Execute $exePath -Argument "--check"
$logonTrigger = New-ScheduledTaskTrigger -AtLogOn
$dailyTrigger = New-ScheduledTaskTrigger -Daily -At "12:00PM"

Register-ScheduledTask -TaskName "BloatBlocker Drift Check" `
    -Action $checkAction `
    -Trigger @($logonTrigger, $dailyTrigger) `
    -Principal $principal `
    -Settings $settings `
    -Force