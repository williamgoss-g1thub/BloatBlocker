<#
  Read-only check: for every registry path/value BloatBlocker's ActionCatalog.cs
  knows about, reports whether the key exists and what the value currently is
  (if anything). Writes nothing. Run this on a real, current Windows install
  before trusting the catalog - Microsoft moves these paths across builds.

  Keep this list in sync with Models/ActionCatalog.cs by hand; there's no
  shared source between the C# and this script in v1.
#>

$checks = @(
    @{ Name = "Copilot policy";        Hive = "HKLM"; Path = "SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot"; Value = "TurnOffWindowsCopilot" }
    @{ Name = "Copilot taskbar";       Hive = "HKCU"; Path = "Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"; Value = "ShowCopilotButton" }
    @{ Name = "Recall data analysis";  Hive = "HKLM"; Path = "SOFTWARE\Policies\Microsoft\Windows\WindowsAI"; Value = "DisableAIDataAnalysis" }
    @{ Name = "Recall enablement";     Hive = "HKLM"; Path = "SOFTWARE\Policies\Microsoft\Windows\WindowsAI"; Value = "AllowRecallEnablement" }
    @{ Name = "Telemetry";             Hive = "HKLM"; Path = "SOFTWARE\Policies\Microsoft\Windows\DataCollection"; Value = "AllowTelemetry" }
    @{ Name = "Cortana in search";     Hive = "HKLM"; Path = "SOFTWARE\Policies\Microsoft\Windows\Windows Search"; Value = "AllowCortana" }
    @{ Name = "OneDrive sync policy";  Hive = "HKLM"; Path = "SOFTWARE\Policies\Microsoft\Windows\OneDrive"; Value = "DisableFileSyncNGSC" }
    @{ Name = "Suggested content 1";   Hive = "HKCU"; Path = "Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"; Value = "SubscribedContent-338388Enabled" }
    @{ Name = "Suggested content 2";   Hive = "HKCU"; Path = "Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"; Value = "SilentInstalledAppsEnabled" }
)

Write-Host "BloatBlocker registry path check (read-only)`n" -ForegroundColor Cyan
Write-Host "Build: $((Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion').DisplayVersion) / $((Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion').CurrentBuildNumber)`n"

foreach ($check in $checks) {
    $fullPath = "$($check.Hive):\$($check.Path)"
    $keyExists = Test-Path $fullPath

    if (-not $keyExists) {
        Write-Host ("[MISSING KEY]  {0,-20} {1}" -f $check.Name, $fullPath) -ForegroundColor Red
        continue
    }

    $prop = Get-ItemProperty -Path $fullPath -Name $check.Value -ErrorAction SilentlyContinue
    if ($null -eq $prop) {
        Write-Host ("[NOT SET]      {0,-20} key exists, value '{1}' not present (expected on a clean/undebloated system)" -f $check.Name, $check.Value) -ForegroundColor Yellow
    } else {
        $current = $prop.$($check.Value)
        Write-Host ("[FOUND]        {0,-20} {1} = {2}" -f $check.Name, $check.Value, $current) -ForegroundColor Green
    }
}

Write-Host "`nA [MISSING KEY] result usually just means BloatBlocker hasn't applied that action yet on this machine - it does not confirm the policy path itself is still correct for this build. Cross-check any you're unsure about against Microsoft's current Group Policy / MDM documentation before relying on it."
