# Dot-source this file: finds makeappx.exe / signtool.exe.
# Preferred source is the Microsoft.Windows.SDK.BuildTools NuGet package the project already restores,
# so CI needs no Visual Studio or Windows SDK install. Falls back to an installed Windows SDK.

function Find-SdkTool([string]$Name) {
    $roots = @()
    $nuget = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $env:USERPROFILE '.nuget\packages' }
    $roots += Join-Path $nuget 'microsoft.windows.sdk.buildtools'
    $roots += "${env:ProgramFiles(x86)}\Windows Kits\10\bin"

    foreach ($root in $roots) {
        if (-not (Test-Path $root)) { continue }
        $hit = Get-ChildItem $root -Recurse -Filter $Name -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -match '[\\/]x64[\\/]' } |
            Sort-Object FullName -Descending | Select-Object -First 1
        if ($hit) { return $hit.FullName }
    }
    throw "Could not find $Name. Run 'dotnet restore UpInstall/UpInstall.csproj' first, or install the Windows SDK."
}

function Invoke-Tool([string]$Exe, [string[]]$Arguments) {
    & $Exe @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$(Split-Path $Exe -Leaf) failed with exit code $LASTEXITCODE" }
}

function Sign-Package([string]$File, [string]$PfxPath, [string]$PfxPassword) {
    if (-not $PfxPath) { return }
    $signtool = Find-SdkTool 'signtool.exe'
    # No timestamp: a self-signed certificate has no timestamp authority behind it.
    Invoke-Tool $signtool @('sign', '/fd', 'SHA256', '/f', $PfxPath, '/p', $PfxPassword, $File)
}
