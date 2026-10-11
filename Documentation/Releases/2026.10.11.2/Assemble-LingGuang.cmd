@echo off
setlocal
set "LINGGUANG_ASSEMBLER=%~f0"
set "LINGGUANG_PACKAGE_DIR=%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$text=[IO.File]::ReadAllText($env:LINGGUANG_ASSEMBLER); $code=($text -split '(?m)^# POWERSHELL_START\r?$')[1]; & ([scriptblock]::Create($code))"
set "LINGGUANG_RESULT=%ERRORLEVEL%"
if not defined LINGGUANG_NO_PAUSE pause
exit /b %LINGGUANG_RESULT%
# POWERSHELL_START
$ErrorActionPreference = 'Stop'
function Get-PackageHash([string] $filePath) {
    $stream = [IO.File]::OpenRead($filePath)
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($algorithm.ComputeHash($stream)).Replace('-','').ToLowerInvariant() }
    finally { $stream.Dispose(); $algorithm.Dispose() }
}
try {
    $folder = [IO.Path]::GetFullPath($env:LINGGUANG_PACKAGE_DIR)
    $parts = @(@{ name='LingGuang-2026.10.11.2-Windows-Qwen.zip.001'; size=1992294400; hash='44675c5272897c3c7bffa04367e4653faa8858a5258111771cd78f6f8c048908' },
@{ name='LingGuang-2026.10.11.2-Windows-Qwen.zip.002'; size=709176386; hash='261842387cc8997b8813cd9351ac6d943b0e9330931aaa2a4a4a3c5f488367c8' })
    $expected = '9729440fb02373ef6ffec24d7b74fb7d32e1b0ab3a31b300b92bf391cb43b5c0'
    $output = Join-Path $folder 'LingGuang-2026.10.11.2-Windows-Qwen.zip'
    foreach ($part in $parts) {
        $path = Join-Path $folder $part.name
        if (!(Test-Path -LiteralPath $path -PathType Leaf)) { throw ('Missing download: ' + $part.name) }
        if ((Get-Item -LiteralPath $path).Length -ne $part.size) { throw ('Incomplete download: ' + $part.name) }
        Write-Host ('Checking ' + $part.name)
        if ((Get-PackageHash $path) -ne $part.hash) { throw ('Download checksum mismatch: ' + $part.name) }
    }
    if (Test-Path -LiteralPath $output) {
        if ((Get-PackageHash $output) -ne $expected) { throw 'A different ZIP already exists. Move it elsewhere before retrying.' }
        Write-Host 'The verified ZIP already exists. Extract it and run LingGuang.exe.'
        exit 0
    }
    $temp = $output + '.' + [Guid]::NewGuid().ToString('N') + '.partial'
    $target = [IO.File]::Open($temp,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try {
        foreach ($part in $parts) {
            Write-Host ('Joining ' + $part.name)
            $sourceStream = [IO.File]::OpenRead((Join-Path $folder $part.name))
            try { $sourceStream.CopyTo($target,1048576) } finally { $sourceStream.Dispose() }
        }
    } finally { $target.Dispose() }
    if ((Get-PackageHash $temp) -ne $expected) { throw 'Joined ZIP checksum mismatch.' }
    [IO.File]::Move($temp,$output)
    Write-Host 'SUCCESS: ZIP verified. Extract the whole ZIP, then run LingGuang.exe.' -ForegroundColor Green
    Write-Host $output
    exit 0
} catch {
    Write-Host ('FAILED: ' + $_.Exception.Message) -ForegroundColor Red
    Write-Host 'Keep both downloaded parts in the same folder as this tool.'
    exit 1
}
