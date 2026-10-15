param([string]$UnityData = 'C:/Program Files/Unity/Hub/Editor/6000.4.5f1/Editor/Data')
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../..'))
$out = Join-Path $PSScriptRoot 'static-check'
[IO.Directory]::CreateDirectory($out) | Out-Null
$refs = @(
    Get-ChildItem -LiteralPath (Join-Path $UnityData 'NetStandard/ref/2.1.0') -Filter '*.dll'
    Get-ChildItem -LiteralPath (Join-Path $UnityData 'Managed/UnityEngine') -Filter '*.dll'
    Get-ChildItem -LiteralPath (Join-Path $project 'Library/ScriptAssemblies') -Filter '*.dll'
)
$lines = @('-nologo', '-target:library', '-nostdlib+', '-define:UNITY_EDITOR', '-langversion:latest', '-nowarn:0436')
$lines += '-out:"' + (Join-Path $out 'Motion013.StaticCheck.dll') + '"'
$lines += $refs | ForEach-Object { '-r:"' + $_.FullName + '"' }
$lines += '"' + (Join-Path $PSScriptRoot 'Motion013DarkButtonFrames.cs') + '"'
foreach ($name in @('XgUi.cs', 'XgButtonPressFeedback.cs', 'XgJuice.cs')) {
    $lines += '"' + (Join-Path $project ('Assets/LingGuangV05/XingGuang/Desktop/' + $name)) + '"'
}
$rsp = Join-Path $out 'compile.rsp'
[IO.File]::WriteAllLines($rsp, $lines)
$log = & (Join-Path $UnityData 'NetCoreRuntime/dotnet.exe') (Join-Path $UnityData 'DotNetSdkRoslyn/csc.dll') ('@' + $rsp) 2>&1
$code = $LASTEXITCODE
$log | Set-Content -LiteralPath (Join-Path $out 'compile.log') -Encoding utf8
Write-Output "Roslyn exit code: $code. Static compilation only; Unity import, runtime and visual review were NOT run."
$log | Write-Output
if ($code -ne 0) { throw "Static compile failed ($code)." }
