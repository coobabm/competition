param(
    [Parameter(Mandatory=$true)][string]$Executable,
    [switch]$ManualAltTab
)
$ErrorActionPreference = 'Stop'
$probeExecutable = (Resolve-Path -LiteralPath $Executable).Path
if ([IO.Path]::GetFileName($probeExecutable) -ne 'NativeLifeWindowProbe.exe') { throw 'Select the dedicated probe executable.' }
$buildRecord = Get-Content -LiteralPath (Join-Path ([IO.Path]::GetDirectoryName($probeExecutable)) 'build-result.json') -Raw | ConvertFrom-Json
if ($buildRecord.result -ne 'Succeeded' -or $buildRecord.company -ne 'LingGuangLocalVerification' -or $buildRecord.product -notlike 'NativeLifeWindowQA-*') { throw 'Missing successful isolated probe build provenance.' }
$runDirectory = Join-Path ([IO.Path]::GetTempPath()) ('LingGuang-NativeLifeRun-' + [guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($runDirectory) | Out-Null
$failures = [Collections.Generic.List[string]]::new()
$assertions = [Collections.Generic.List[object]]::new()
$probeProcess = $null
$probeHwnd = [IntPtr]::Zero
$oldRun = $env:LINGGUANG_NATIVE_QA_RUN
if (-not ('NativeLifeProbeWin32' -as [type])) { Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class NativeLifeProbeWin32 {
 delegate bool EnumCallback(IntPtr window, IntPtr data);
 [DllImport("user32.dll")] static extern bool EnumWindows(EnumCallback callback, IntPtr data);
 [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr window);
 [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr window);
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
 [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetClassNameW(IntPtr window, StringBuilder name, int count);
 [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr window, int command);
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
 public static IntPtr FindOwnUnityWindow(uint processId) {
   IntPtr found = IntPtr.Zero;
   EnumWindows((window, data) => {
     uint owner; GetWindowThreadProcessId(window, out owner);
     if (owner != processId) return true;
     var name = new StringBuilder(64); GetClassNameW(window, name, name.Capacity);
     if (name.ToString() != "UnityWndClass") return true;
     found = window; return false;
   }, IntPtr.Zero);
   return found;
 }
}
'@
}
function Assert-ProbeWindow {
    $probeProcess.Refresh()
    if ($probeProcess.HasExited -or $probeHwnd -eq [IntPtr]::Zero -or -not [NativeLifeProbeWin32]::IsWindow($probeHwnd)) { throw 'Probe window is unavailable.' }
    [uint32]$ownerProcess = 0
    [NativeLifeProbeWin32]::GetWindowThreadProcessId($probeHwnd, [ref]$ownerProcess) | Out-Null
    $className = [Text.StringBuilder]::new(64)
    [NativeLifeProbeWin32]::GetClassNameW($probeHwnd, $className, $className.Capacity) | Out-Null
    if ($ownerProcess -ne $probeProcess.Id -or $className.ToString() -ne 'UnityWndClass') { throw 'Refusing to operate a window outside the verified probe PID / UnityWndClass.' }
    if ($probeProcess.MainModule.FileName -ne $probeExecutable) { throw 'Probe executable identity changed.' }
}
function Set-Phase([string]$Value) {
    [IO.File]::WriteAllText((Join-Path $runDirectory 'phase.txt'), $Value, [Text.UTF8Encoding]::new($false))
}
function Read-Frames {
    $framePath = Join-Path $runDirectory 'frames.jsonl'
    if (-not (Test-Path -LiteralPath $framePath)) { return @() }
    $result = @()
    $stream = [IO.File]::Open($framePath, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
    $reader = [IO.StreamReader]::new($stream)
    try { $lines = $reader.ReadToEnd() -split "`n" } finally { $reader.Dispose() }
    foreach ($line in $lines) {
        try { if ($line.Trim()) { $result += ($line | ConvertFrom-Json) } } catch { } # A concurrent final partial line is retried next poll.
    }
    return $result
}
function Wait-Until([scriptblock]$Condition, [string]$Label, [int]$Seconds = 12) {
    $deadline = [DateTime]::UtcNow.AddSeconds($Seconds)
    do {
        if (& $Condition) { return }
        if ($null -ne $probeProcess) { $probeProcess.Refresh(); if ($probeProcess.HasExited) { throw "Probe exited while waiting: $Label" } }
        Start-Sleep -Milliseconds 150
    } while ([DateTime]::UtcNow -lt $deadline)
    throw "Timed out: $Label"
}
function Check([bool]$Ok, [string]$Label) {
    $assertions.Add([pscustomobject]@{ name=$Label; passed=$Ok })
    if (-not $Ok) { $failures.Add($Label) }
}
function Stable($Row) { return $Row.nativeBefore -eq $Row.nativeAfter -and $Row.bridgeBefore -eq $Row.bridgeAfter -and $Row.nativeBefore -eq $Row.bridgeBefore }
try {
    $env:LINGGUANG_NATIVE_QA_RUN = $runDirectory
    $playerLog = Join-Path $runDirectory 'Player.log'
    $probeProcess = Start-Process -FilePath $probeExecutable -ArgumentList @('-screen-fullscreen','0','-screen-width','800','-screen-height','600','-logFile',('"' + $playerLog + '"')) -WindowStyle Hidden -PassThru
    Wait-Until { [NativeLifeProbeWin32]::FindOwnUnityWindow([uint32]$probeProcess.Id) -ne [IntPtr]::Zero } 'native probe HWND' 30
    $probeHwnd = [NativeLifeProbeWin32]::FindOwnUnityWindow([uint32]$probeProcess.Id)
    Assert-ProbeWindow
    [NativeLifeProbeWin32]::ShowWindow($probeHwnd, 9) | Out-Null # SW_RESTORE; own verified HWND only.
    Assert-ProbeWindow
    [NativeLifeProbeWin32]::SetForegroundWindow($probeHwnd) | Out-Null
    Set-Phase 'focused'
    Wait-Until { @(Read-Frames | Where-Object { $_.phase -eq 'focused' -and $_.focused -and $_.active -and -not $_.nativeAfter }).Count -ge 8 } 'focused active samples'
    Start-Sleep -Seconds 2

    Set-Phase 'minimized'
    Assert-ProbeWindow
    [NativeLifeProbeWin32]::ShowWindow($probeHwnd, 6) | Out-Null # SW_MINIMIZE
    Wait-Until { @(Read-Frames | Where-Object { $_.nativeAfter -and $_.bridgeAfter -and -not $_.active }).Count -ge 8 } 'minimized inactive samples'
    Start-Sleep -Seconds 2

    Set-Phase 'restored-unfocused'
    Assert-ProbeWindow
    [NativeLifeProbeWin32]::ShowWindow($probeHwnd, 4) | Out-Null # SW_SHOWNOACTIVATE: show restored window without activating another window.
    Wait-Until { @(Read-Frames | Where-Object { $_.phase -eq 'restored-unfocused' -and -not $_.nativeAfter -and -not $_.focused -and $_.active }).Count -ge 8 } 'visible unfocused active samples'
    Start-Sleep -Seconds 2

    Set-Phase 'longframe'
    Wait-Until { @(Read-Frames | Where-Object { $_.phase -eq 'longframe' -and $_.unityDelta -ge 1 -and $_.active -and -not $_.nativeAfter }).Count -ge 1 } 'ordinary long frame remains active'

    if ($ManualAltTab) {
        Assert-ProbeWindow
        [NativeLifeProbeWin32]::SetForegroundWindow($probeHwnd) | Out-Null
        Set-Phase 'alt-tab'
        Wait-Until { @(Read-Frames | Where-Object { $_.phase -eq 'alt-tab' -and $_.focused -and $_.active }).Count -ge 5 } 'pre Alt-Tab focused samples'
        Write-Host 'Alt-Tab away from the blue probe window now; keep it unminimized for three seconds. Only your keypress changes other application focus.'
        Wait-Until { @(Read-Frames | Where-Object { $_.phase -eq 'alt-tab' -and -not $_.focused -and -not $_.nativeAfter -and $_.active }).Count -ge 15 } 'manual Alt-Tab unfocused samples' 25
        Start-Sleep -Seconds 2
    }

    Set-Phase 'quit'
    if (-not $probeProcess.WaitForExit(15000)) { throw 'Probe did not exit after quit command.' }
    $rows = @(Read-Frames)
    $identity = Get-Content -LiteralPath (Join-Path $runDirectory 'identity.json') -Raw | ConvertFrom-Json
    $completion = Get-Content -LiteralPath (Join-Path $runDirectory 'complete.json') -Raw | ConvertFrom-Json
    Check ($probeProcess.ExitCode -eq 0 -and $completion.ok -and $completion.errors -eq 0) 'Player exits cleanly without runtime errors'
    Check ($identity.processId -eq $probeProcess.Id -and $identity.company -eq $buildRecord.company -and $identity.product -eq $buildRecord.product) 'Identity matches isolated build and launched PID'
    Check ([bool]$identity.runInBackground) 'Actual player runInBackground enabled'
    $settled = @($rows | Where-Object { $_.elapsed -ge 2 })
    Check (@($settled | Where-Object { $_.nativeBefore -eq $_.nativeAfter -and ($_.nativeBefore -ne $_.bridgeBefore -or $_.nativeAfter -ne $_.bridgeAfter) }).Count -eq 0) 'Bridge agrees with native IsIconic on stable native samples'
    $minimized = @($rows | Where-Object { (Stable $_) -and $_.nativeAfter })
    Check ($minimized.Count -ge 8 -and @($minimized | Where-Object { $_.active }).Count -eq 0) 'Minimized samples all inactive'
    if ($minimized.Count -ge 2) {
        Check (($minimized[-1].elapsed - $minimized[0].elapsed) -ge 2 -and [Math]::Abs($minimized[-1].activeSeconds - $minimized[0].activeSeconds) -lt 0.000001) 'Harness active-time accumulator freezes over minimized interval'
    }
    $recovery = $null; $afterRecovery = $null
    for ($index = 1; $index -lt ($rows.Count - 1); $index++) {
        if ((Stable $rows[$index-1]) -and $rows[$index-1].nativeAfter -and (Stable $rows[$index]) -and -not $rows[$index].nativeAfter) { $recovery = $rows[$index]; $afterRecovery = $rows[$index+1]; break }
    }
    Check ($null -ne $recovery -and -not $recovery.active) 'First observed restore frame skipped'
    Check ($null -ne $afterRecovery -and (Stable $afterRecovery) -and -not $afterRecovery.nativeAfter -and $afterRecovery.active) 'Following visible frame resumes active cadence'
    $unfocused = @($rows | Where-Object { $_.phase -eq 'restored-unfocused' -and (Stable $_) -and -not $_.nativeAfter -and -not $_.focused -and $_.active })
    Check ($unfocused.Count -ge 8 -and ($unfocused[-1].elapsed - $unfocused[0].elapsed) -ge 2 -and $unfocused[-1].activeSeconds -gt $unfocused[0].activeSeconds) 'Visible unfocused window continues active time'
    Check (@($rows | Where-Object { $_.phase -eq 'longframe' -and $_.unityDelta -ge 1 -and (Stable $_) -and -not $_.nativeAfter -and $_.active }).Count -ge 1) 'Ordinary long frame remains active'
    if ($ManualAltTab) { Check (@($rows | Where-Object { $_.phase -eq 'alt-tab' -and -not $_.focused -and -not $_.nativeAfter -and $_.active }).Count -ge 15) 'Manual Alt-Tab loss of focus remains active' }
    Check (@($rows | Where-Object { $_.conflict }).Count -eq 0) 'Empty scene introduces no event conflict'
} catch {
    $failures.Add($_.Exception.ToString())
} finally {
    if ($null -ne $probeProcess) {
        $probeProcess.Refresh()
        if (-not $probeProcess.HasExited) {
            try { Assert-ProbeWindow; [NativeLifeProbeWin32]::ShowWindow($probeHwnd, 4) | Out-Null } catch { $failures.Add('Restore failed: ' + $_.Exception.Message) }
            Set-Phase 'quit'
            if (-not $probeProcess.WaitForExit(5000)) { $probeProcess.Kill(); $probeProcess.WaitForExit() }
        }
    }
    $env:LINGGUANG_NATIVE_QA_RUN = $oldRun
    $report = [ordered]@{ passed=($failures.Count -eq 0); executable=$probeExecutable; directory=$runDirectory; manualAltTab=[bool]$ManualAltTab; assertions=@($assertions.ToArray()); failures=@($failures.ToArray()); scope='Real bridge and ActiveClock in isolated scene; accumulator is harness-only, not whole-game cooldown/save acceptance.' }
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $runDirectory 'assertion-report.json') -Encoding UTF8
    Write-Host ('NATIVE_LIFE_WINDOW_REPORT ' + (Join-Path $runDirectory 'assertion-report.json'))
}
if ($failures.Count -gt 0) { throw ('Native window probe failed: ' + ($failures -join '; ')) }
Write-Host 'NATIVE_LIFE_WINDOW_PASS'
