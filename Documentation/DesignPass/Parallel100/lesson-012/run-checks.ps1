$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    $env:DOTNET_CLI_HOME = $PSScriptRoot
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
    $env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
    $env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = 'false'
    $env:NUGET_PACKAGES = Join-Path $PSScriptRoot '.packages'
    dotnet run --project Lesson012.csproj --configfile NuGet.Config
    if ($LASTEXITCODE -ne 0) { throw "Lesson012 checks failed: $LASTEXITCODE" }
} finally { Pop-Location }
