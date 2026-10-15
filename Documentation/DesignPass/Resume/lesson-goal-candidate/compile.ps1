$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
$destination = Join-Path $PSScriptRoot 'compile'
New-Item -ItemType Directory -Force -Path $destination | Out-Null
$destination = $destination.Replace('\','/')
$sources = (Join-Path $PSScriptRoot 'sources').Replace('\','/')
$partial = (Join-Path $PSScriptRoot 'DesignWallpaperSmoke.LessonGoal.cs').Replace('\','/')
$bee = 'Library/Bee/artifacts/1900b0aE.dag'
$compiler = 'C:/Program Files/Unity/Hub/Editor/6000.4.5f1/Editor/Data/DotNetSdkRoslyn/csc.dll'
$dotnet = 'C:/Program Files/Unity/Hub/Editor/6000.4.5f1/Editor/Data/NetCoreRuntime/dotnet.exe'
Push-Location $root
try {
    foreach ($assembly in @('Assembly-CSharp', 'Assembly-CSharp-Editor')) {
        $rsp = [IO.File]::ReadAllText((Join-Path $root "$bee/$assembly.rsp"))
        $rsp = $rsp.Replace('-out:"' + $bee + '/' + $assembly + '.dll"', '-out:"' + $destination + '/' + $assembly + '.dll"')
        $rsp = $rsp.Replace('-refout:"' + $bee + '/' + $assembly + '.ref.dll"', '-refout:"' + $destination + '/' + $assembly + '.ref.dll"')
        foreach ($source in @('XgLabelPage.cs','XgTeachCompanion.cs')) {
            $rsp = $rsp.Replace('"Assets/LingGuangV05/XingGuang/Desktop/' + $source + '"', '"' + $sources + '/' + $source + '"')
        }
        foreach ($source in @('DesignWallpaperSmoke.FirstMinute.cs','DesignWallpaperSmoke.cs')) {
            $rsp = $rsp.Replace('"Assets/LocalBuild/Editor/' + $source + '"', '"' + $sources + '/' + $source + '"')
        }
        if ($assembly -eq 'Assembly-CSharp-Editor') {
            $rsp = $rsp.Replace('-r:"' + $bee + '/Assembly-CSharp.ref.dll"', '-r:"' + $destination + '/Assembly-CSharp.ref.dll"')
            $rsp += "`n" + '"' + $partial + '"' + "`n"
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
