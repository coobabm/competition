$ErrorActionPreference = 'Stop'
$taskDirectory = $PSScriptRoot
$projectDirectory = [IO.Path]::GetFullPath((Join-Path $taskDirectory '../../../..'))
$unityData = 'C:/Program Files/Unity/Hub/Editor/6000.4.5f1/Editor/Data'
$responsePath = Join-Path $taskDirectory 'candidate-compile.rsp'
$outputPath = Join-Path $taskDirectory 'candidate-static-check.dll'
$references = @(
    Get-ChildItem -LiteralPath "$unityData/NetStandard/ref/2.1.0" -Filter '*.dll'
    Get-ChildItem -LiteralPath "$unityData/Managed/UnityEngine" -Filter '*.dll'
    Get-ChildItem -LiteralPath "$projectDirectory/Library/ScriptAssemblies" -Filter '*.dll'
)
$arguments = @('-nologo', '-target:library', '-nostdlib+', '-define:UNITY_EDITOR', '-nowarn:0436', "-out:`"$outputPath`"")
$arguments += $references | ForEach-Object { '-r:"' + $_.FullName + '"' }
$arguments += '"' + (Join-Path $taskDirectory 'XgButtonPressFeedback.candidate.cs') + '"'
$arguments += '"' + (Join-Path $taskDirectory 'Motion005CanvasGroupChecks.cs') + '"'
[IO.File]::WriteAllLines($responsePath, $arguments)
[IO.File]::WriteAllText((Join-Path $taskDirectory 'static-compile.log'), '')
& "$unityData/NetCoreRuntime/dotnet.exe" "$unityData/DotNetSdkRoslyn/csc.dll" "@$responsePath" 2>&1 | Tee-Object -FilePath (Join-Path $taskDirectory 'static-compile.log')
$compilerExit = $LASTEXITCODE
Add-Content -LiteralPath (Join-Path $taskDirectory 'static-compile.log') -Value "Compiler exit code: $compilerExit. Static compilation only; no Unity process or event checks executed."
exit $compilerExit
