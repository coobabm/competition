$ErrorActionPreference = 'Stop'
$deliveryRoot = $PSScriptRoot
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $deliveryRoot '../../../..')).Path
$unityData = 'C:/Program Files/Unity/Hub/Editor/6000.4.5f1/Editor/Data'
$arguments = @('-nologo', '-target:library', '-nostdlib+', '-nowarn:0436',
    ('-out:"' + (Join-Path $deliveryRoot 'motion-003-static-check.dll') + '"'),
    ('-r:"' + $unityData + '/NetStandard/ref/2.1.0/netstandard.dll"'))
$referenceFiles = @(Get-ChildItem -LiteralPath "$unityData/Managed/UnityEngine" -Filter '*.dll')
$referenceFiles += Get-Item -LiteralPath "$projectRoot/Library/ScriptAssemblies/Assembly-CSharp.dll"
$referenceFiles += Get-Item -LiteralPath "$projectRoot/Library/ScriptAssemblies/UnityEngine.UI.dll"
foreach ($referenceFile in $referenceFiles) { $arguments += '-r:"' + $referenceFile.FullName + '"' }
$arguments += '"' + (Join-Path $deliveryRoot 'XgButtonPressFeedback.candidate.cs') + '"'
$arguments += '"' + (Join-Path $deliveryRoot 'Motion003PointerChecks.cs') + '"'
$responsePath = Join-Path $deliveryRoot 'compile.rsp'
[IO.File]::WriteAllLines($responsePath, $arguments)
[IO.File]::WriteAllText((Join-Path $deliveryRoot 'compile.log'), '')
& "$unityData/NetCoreRuntime/dotnet.exe" "$unityData/DotNetSdkRoslyn/csc.dll" -noconfig "@$responsePath" 2>&1 |
    Tee-Object -FilePath (Join-Path $deliveryRoot 'compile.log')
$compileExitCode = $LASTEXITCODE
Add-Content -LiteralPath (Join-Path $deliveryRoot 'compile.log') -Value "Compiler exit code: $compileExitCode. Static compilation only; no Unity execution."
exit $compileExitCode
