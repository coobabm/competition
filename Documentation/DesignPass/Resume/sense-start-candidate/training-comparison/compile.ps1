$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../../../..')).Path
$destination = Join-Path $PSScriptRoot 'compile'
New-Item -ItemType Directory -Force -Path $destination | Out-Null
$destination = $destination.Replace('\','/')
$sources = (Join-Path $PSScriptRoot 'sources').Replace('\','/')
$bee = 'Library/Bee/artifacts/1900b0aE.dag'
$compiler = 'C:/Program Files/Unity/Hub/Editor/6000.4.5f1/Editor/Data/DotNetSdkRoslyn/csc.dll'
$dotnet = 'C:/Program Files/Unity/Hub/Editor/6000.4.5f1/Editor/Data/NetCoreRuntime/dotnet.exe'
Push-Location $root
try {
    foreach ($assembly in @('LingGuangV05.XingGuang.Core', 'Assembly-CSharp', 'Assembly-CSharp-Editor')) {
        $rsp = [IO.File]::ReadAllText((Join-Path $root "$bee/$assembly.rsp"))
        $rsp = $rsp.Replace('-out:"' + $bee + '/' + $assembly + '.dll"', '-out:"' + $destination + '/' + $assembly + '.dll"')
        $rsp = $rsp.Replace('-refout:"' + $bee + '/' + $assembly + '.ref.dll"', '-refout:"' + $destination + '/' + $assembly + '.ref.dll"')
        $rsp = $rsp.Replace('"Assets/LingGuangV05/XingGuang/Core/XgSim.Teaching.cs"', '"' + $sources + '/XgSim.Teaching.cs"')
        $rsp = $rsp.Replace('"Assets/LingGuangV05/XingGuang/Desktop/XgTrainPage.Side.cs"', '"' + $sources + '/XgTrainPage.Side.cs"')
        $rsp = $rsp.Replace('"Assets/LocalBuild/Editor/DesignWallpaperSmoke.SenseStart.cs"', '"' + $sources + '/DesignWallpaperSmoke.SenseStart.cs"')
        $rsp = $rsp.Replace('-r:"' + $bee + '/LingGuangV05.XingGuang.Core.ref.dll"', '-r:"' + $destination + '/LingGuangV05.XingGuang.Core.ref.dll"')
        if ($assembly -eq 'Assembly-CSharp-Editor') {
            $rsp = $rsp.Replace('-r:"' + $bee + '/Assembly-CSharp.ref.dll"', '-r:"' + $destination + '/Assembly-CSharp.ref.dll"')
        }
        if ($rsp -match '(?m)^-(refout|out):"Library/') { throw 'Output would escape the candidate directory' }
        $responsePath = "$destination/$assembly.rsp"
        [IO.File]::WriteAllText($responsePath, $rsp)
        & $dotnet $compiler ('@' + $responsePath) *> "$destination/$assembly.log"
        $result = $LASTEXITCODE
        if ($result -ne 0) { Get-Content "$destination/$assembly.log"; throw "Compile failed: $assembly ($result)" }
        Write-Output "COMPILE_PASS $assembly"
    }
} finally { Pop-Location }
