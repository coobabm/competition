param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path)
$ErrorActionPreference = 'Stop'
# Pure production scheduling policy only: no Unity, service startup or save access.
$source = Join-Path $ProjectRoot 'Assets/LingGuangV05/Core/Chat/LlmScheduling.cs'
Add-Type -Path $source
$script:checks = 0
function Assert-Equal($actual, $expected, [string]$name) {
    if ($actual -ne $expected) { throw "$name : expected $expected, got $actual" }
    $script:checks++
}
$policy = [LingGuangV05.Core.Chat.LlmScheduling]
Assert-Equal ($policy::SlotCount($false, $false)) 3 'Owned production before purchase: total slots'
Assert-Equal ($policy::SlotCount($true, $false)) 4 'Owned production after purchase: total slots'
Assert-Equal ($policy::JudgeLimit(3, $true)) 1 'Owned production before purchase: judges'
Assert-Equal ($policy::JudgeLimit(4, $true)) 2 'Owned production after purchase: judges'
Assert-Equal ($policy::SlotCount($false, $true)) 1 'QA default is not production default'
Assert-Equal ($policy::JudgeLimit(4, $false)) 3 'Unrouted QA upper bound is not production bound'
Assert-Equal ($policy::JudgeLimit(1, $false)) 1 'External single slot stays serial'
Assert-Equal ($policy::JudgeLimit(2, $false)) 1 'External two slots still have one judge'
$seat = [LingGuangV05.Core.Chat.LlmSeat]::Others
$lane = [LingGuangV05.Core.Chat.LlmLane]::Background
$empty = [Func[int,bool]] { param($slot) $false }
$lastBusy = [Func[int,bool]] { param($slot) $slot -eq 3 }
$backgroundBusy = [Func[int,bool]] { param($slot) $slot -ge 2 }
Assert-Equal ($policy::PickSlot($seat, $lane, 4, $true, 0, $empty)) 3 'First judge selects slot 3'
Assert-Equal ($policy::PickSlot($seat, $lane, 4, $true, 1, $lastBusy)) 2 'Second judge selects slot 2'
Assert-Equal ($policy::PickSlot($seat, $lane, 4, $true, 2, $backgroundBusy)) -2 'Third judge cannot borrow idle character slots'
Assert-Equal ($policy::BackgroundMayStart(0, 0, $false)) $true 'Idle allows dispatch'
Assert-Equal ($policy::BackgroundMayStart(1, 0, $false)) $false 'Queued visible chat blocks new background dispatch'
Assert-Equal ($policy::BackgroundMayStart(0, 1, $false)) $false 'Running visible chat blocks new background dispatch'
Assert-Equal ($policy::BackgroundMayStart(0, 0, $true)) $false 'Typing blocks new background dispatch'
"POLICY_CHECKS_OK $script:checks (pure scheduling only; no Unity/game/visual validation)"
