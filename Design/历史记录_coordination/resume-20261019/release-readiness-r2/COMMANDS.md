# Commands for parent execution, not executed by this audit

2026-10-19 final execution checklist; **none of these commands was executed in this document update**. D1/casino acceptance is still pending; the fresh Player, audio and native inference are unverified; no new release is published. Claude E1/E2/E3 remain separately assigned. Font manifest synchronization has been reported by its owner, but independent audit is pending; do not infer a pass or repeat that audit here.

Run only after required serial UI acceptance and the independent font/notice review, all project Editor/Player/model workloads have exited, and production source is frozen. Continue in one PowerShell session. Support only the current latest version; preserve `DefaultCompany/competition`. No old-version migration, legacy-save search, identity change or ordinary-save repair is part of this checklist. Do not substitute historical binaries or old WindowsReleaseBuild.

Use **sky** for actual UI observation, focus, clicking, typing and screenshots. The consistency checklist's `Player-WindowQa.ps1`/PowerShell window assistant snippets are superseded and must not be executed. PowerShell below is for future process launch, scoped evidence collection and packaging only. If sky is unavailable, leave interactive acceptance pending rather than changing UI tools.

```powershell
$ErrorActionPreference = 'Stop'
$project = 'C:/Users/xomem/Documents/ChatGPT/games/competition-latest-20261016'
$unity = 'C:/Program Files/Unity/Hub/Editor/6000.4.5f1/Editor/Unity.exe'
$runId = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,8)
$buildLog = 'C:/Users/xomem/Documents/ChatGPT/games/.coordination/resume-20261019/release-readiness-r2/build-' + $runId + '.log'
$buildStart = [DateTime]::UtcNow
$buildProcess = Start-Process -FilePath $unity -WindowStyle Hidden -Wait -PassThru -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$project+'"'),'-executeMethod','IntegratedReleaseBuild.Run','-logFile',('"'+$buildLog+'"'))
if ($buildProcess.ExitCode -ne 0) { throw 'Build failed; inspect the complete log' }
$record = Get-Content -LiteralPath (Join-Path $project 'LocalVerification/Release/latest-build.json') -Raw | ConvertFrom-Json
$release = $record.path
if ($record.result -ne 'Succeeded' -or $record.errors -ne 0 -or $record.version -ne '2026.10.19.1' -or [DateTime]::Parse($record.utc).ToUniversalTime() -lt $buildStart) { throw 'Missing current successful build record' }
if ($record.company -cne 'DefaultCompany' -or $record.product -cne 'competition') { throw 'Unexpected release identity; do not inspect another save or prefs root' }
if (-not (Get-Content -LiteralPath $buildLog -Raw).Contains('INTEGRATED_RELEASE_BUILD_PASS '+$release)) { throw 'This log does not confirm this build directory' }
$build = Join-Path $release 'build-result.json'
$tools = 'C:/Users/xomem/Documents/ChatGPT/games/.coordination/integration-20261018/release-smoke-current-stage'
$evidence = 'C:/Users/xomem/Documents/ChatGPT/games/LocalVerification/ReleaseSmoke/' + $runId
New-Item -ItemType Directory -Path $evidence -ErrorAction Stop | Out-Null
$save = Join-Path $evidence 'PlayerData' # Leave nonexistent for the real Player's validated marker creation.
$prefs = Join-Path $tools 'PlayerPrefs-Guard.ps1'
```

The user authorized ordinary-save **read-only protection verification**. That covers existence, relative filenames, byte lengths and SHA256 comparison within the current identity's `LocalLow/DefaultCompany/competition/LingGuangV05` tree before/after a separately authorized isolated run. It does not authorize loading those saves into the game, dumping their contents, recovery, copying, moving, rewriting, deleting, migration or searching other identities. Reports go only into the new evidence directory. Do not enumerate other LocalLow siblings. Missing roots remain missing.

This bounded comparison covers all files in that save tree, including `chapter-one.sav` variants, `player-profile.json` variants, slots, chapters, endings and content-addressed `snapshot-archive` blocks. The older extension-only filter missed snapshot blocks. It does not verify DreamOS profiles outside this tree, music/cache directories or all user files. Reparse points in the root's ancestors or descendants abort the pass; no link is intentionally followed. Run both fingerprints with all game processes stopped, preserve a mismatch and report it rather than repairing the ordinary tree.

```powershell
$localLow = Join-Path (Split-Path ([Environment]::GetFolderPath('LocalApplicationData')) -Parent) 'LocalLow'
$saveRoots = @((Join-Path $localLow 'DefaultCompany/competition/LingGuangV05'))
function Get-SaveFingerprint {
    $roots = @(); $files = @()
    foreach ($root in $saveRoots) {
        $ancestors = [Collections.Generic.Stack[string]]::new()
        $ancestor = $root
        while ($ancestor) {
            $ancestors.Push($ancestor)
            $ancestor = Split-Path -Path $ancestor -Parent
        }
        while ($ancestors.Count -gt 0) {
            $ancestor = $ancestors.Pop()
            if (Test-Path -LiteralPath $ancestor) {
                if ((Get-Item -LiteralPath $ancestor -Force -ErrorAction Stop).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Reparse save ancestor: $ancestor" }
            }
        }
        if ((Test-Path -LiteralPath $root) -and -not (Test-Path -LiteralPath $root -PathType Container)) { throw "Save root is not a directory: $root" }
        $exists = Test-Path -LiteralPath $root -PathType Container
        $roots += [pscustomobject]@{path=$root;exists=$exists}
        if (-not $exists) { continue }
        $todo = [Collections.Generic.Stack[string]]::new(); $todo.Push($root)
        while ($todo.Count -gt 0) {
            $folder = $todo.Pop()
            if ((Get-Item -LiteralPath $folder).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Reparse save path: $folder" }
            foreach ($item in Get-ChildItem -LiteralPath $folder -Force -ErrorAction Stop) {
                if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Reparse save entry: $($item.FullName)" }
                if ($item.PSIsContainer) { $todo.Push($item.FullName); continue }
                $files += [pscustomobject]@{root=$root;path=$item.FullName.Substring($root.Length);bytes=$item.Length;sha256=(Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256 -ErrorAction Stop).Hash}
            }
        }
    }
    [ordered]@{roots=@($roots | Sort-Object path);files=@($files | Sort-Object root,path)} | ConvertTo-Json -Depth 6
}
Get-SaveFingerprint | Set-Content -LiteralPath (Join-Path $evidence 'ordinary-saves-before.json') -Encoding utf8
```

The exact registry identity is `HKCU\Software\DefaultCompany\competition`; the guard obtains it from the final build record. The verification flag **does not redirect PlayerPrefs**: ordinary startup can write this key. Snapshot/Compare read the exact key; Restore writes/deletes values and may remove an originally absent product key. Ordinary-save read-only permission does not by itself authorize any registry access or those temporary PlayerPrefs changes. The following launch/prefs sequence is a plan only, contingent on the parent establishing that exact registry/Player scope is authorized. Do not skip protection or launch unisolated to bypass this gate. Keep its private CLIXML backup local, outside the release payload.

```powershell
$prefsScopeAuthorized = $false # Parent changes only after confirming recorded authorization for this exact scope.
if (-not $prefsScopeAuthorized) { throw 'Player startup and exact-key PlayerPrefs snapshot/restore scope are not cleared by save-read-only permission' }
& $prefs -Action Snapshot -BuildResult $build -SnapshotPath (Join-Path $evidence 'prefs-before.clixml') | Set-Content -LiteralPath (Join-Path $evidence 'prefs-snapshot.json') -Encoding utf8
if (-not $?) { throw 'PlayerPrefs snapshot failed; do not launch' }
$exe = Join-Path $release 'MyComputerAlive.exe'
$log = Join-Path $evidence 'player.log'
Copy-Item -LiteralPath $build,(Join-Path $release 'source-provenance.json') -Destination $evidence
Get-FileHash -LiteralPath $exe -Algorithm SHA256 | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $evidence 'exe-hash.json') -Encoding utf8
# This is the intentionally visible interactive Player; operate it with sky.
$player = Start-Process -FilePath $exe -WorkingDirectory $release -WindowStyle Normal -PassThru -ArgumentList @('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','--verification-save-dir',('"'+$save+'"'),'-logFile',('"'+$log+'"'))
```

Follow only the behavioral/native/audio evidence requirements in the consistency-stage checklist; do not rerun its setup, snapshot or window-helper blocks. Use the existing `$evidence`, `$save` and `$player` above. Before UI writes confirm the exact `VERIFICATION_SAVE_ROOT` log and `.lingguang-verification-save` marker. All test save/profile/cache/music writes must remain in isolated PlayerData. sky must observe the current window and controls for each action. Use a visible generic private contact (`oldfriend` or script-free `cousin`) for the deliberate chat, **not Qingwen**, whose route bypasses the required hub trace. Retain complete same-ID requirements with contactKind=`other`; if no eligible contact is visible, record the limitation without injecting progression.

The actual read-only trace helper parameters are `-TracePath <exact path printed in this Player log> -PlayerLog $log`. The audio Python helper accepts `--inspect`, or `--record --seconds 15 --output <new absolute WAV>`; recording is not implied by inspection. Use the existing audio-env Python and hidden background capture as documented; operate the music UI through sky and listen to the original WAV. No measured signal statistic alone proves continuity. A successful ordinary chat does not prove cancellation; unexercised paths remain unverified.

Only after normal Player exit and no concurrent game process:

```powershell
if (-not $player.HasExited) { throw 'Player has not exited; do not restore preferences' }
& $prefs -Action Compare -BuildResult $build -SnapshotPath (Join-Path $evidence 'prefs-before.clixml') | Set-Content -LiteralPath (Join-Path $evidence 'prefs-after-player.json') -Encoding utf8
& $prefs -Action Restore -BuildResult $build -SnapshotPath (Join-Path $evidence 'prefs-before.clixml') -ExitedPlayerProcessId $player.Id | Set-Content -LiteralPath (Join-Path $evidence 'prefs-restored.json') -Encoding utf8
if (-not $?) { throw 'PlayerPrefs restoration failed; preserve backup and evidence' }
& $prefs -Action Compare -BuildResult $build -SnapshotPath (Join-Path $evidence 'prefs-before.clixml') | Set-Content -LiteralPath (Join-Path $evidence 'prefs-final-compare.json') -Encoding utf8
if ((Get-Content -LiteralPath (Join-Path $evidence 'prefs-final-compare.json') -Raw | ConvertFrom-Json).unchanged -ne $true) { throw 'PlayerPrefs final comparison failed' }
Get-SaveFingerprint | Set-Content -LiteralPath (Join-Path $evidence 'ordinary-saves-after.json') -Encoding utf8
if ((Get-Content -LiteralPath (Join-Path $evidence 'ordinary-saves-before.json') -Raw) -cne (Get-Content -LiteralPath (Join-Path $evidence 'ordinary-saves-after.json') -Raw)) { throw 'Ordinary save fingerprints changed; preserve both reports' }
```

After all required acceptance, independent font/notice closure, successful smoke and native shutdown, use the pinned packager below. Both the final-candidate path and group-art/release-review copy matched this new SHA256 during this document update. The old `42769a...` checksum in the consistency checklist is superseded. Its five mandatory parameters are shown; optional `-SevenZip` defaults to `C:\Program Files\7-Zip\7z.exe`. Output must be a new directory outside the build. This future script hashes the model, copies the payload/notices, creates split 7z volumes and tests archive integrity; it performs no upload. No model hashing or packaging was done here.

```powershell
$packager = 'C:/Users/xomem/Documents/ChatGPT/games/.coordination/resume-20261019/release-package-final-candidate/Package-VerifiedRelease-LowLoad.ps1'
if ((Get-FileHash -LiteralPath $packager -Algorithm SHA256).Hash -ne 'f1c46fdbb9f0089a0e5b248b5a7659d19844f2abfae23ffa273850447dad60f7') { throw 'Packager changed; re-review before packaging' }
& $packager -BuildDirectory $release -OutputDirectory ('C:/Users/xomem/Documents/ChatGPT/games/LocalVerification/ReleasePackage/'+$runId) -ProjectDirectory $project -LlmUnityDirectory (Join-Path $project 'Library/PackageCache/ai.undream.llm@9931a9ea4690') -NativeLicenseDirectory 'C:/Users/xomem/Documents/ChatGPT/games/.coordination/integration-20261018/release-audit-stage/native-licenses'
```

Preserve package-result.json, exact source linkage, volume checksums and 7z test results, then inspect final notices/payload and have the parent decide publication. Archive integrity is not game acceptance. These are inspected execution instructions, not evidence that the build, prefs restoration, smoke, native response, audio or package has passed. No command above was executed by this audit.
