param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path)
$ErrorActionPreference = 'Stop'
$baseline = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'source-baseline.json') -Raw | ConvertFrom-Json
$stale = $false
foreach ($entry in $baseline) {
    $path = Join-Path $ProjectRoot $entry.path
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Write-Output ('MISSING ' + $entry.path)
        $stale = $true
        continue
    }
    $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    if ($actual -ne $entry.sha256) {
        Write-Output ('STALE ' + $entry.path + ' expected=' + $entry.sha256 + ' actual=' + $actual)
        $stale = $true
    }
}
if ($stale) { exit 2 }
Write-Output ('SOURCE_BASELINE_UNCHANGED: ' + $baseline.Count + ' files; static fingerprint check only; scene NOT_RUN.')
exit 0
