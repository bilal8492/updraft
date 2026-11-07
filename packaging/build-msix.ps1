<#
.SYNOPSIS
  Packs a published (self-contained) Updraft build into an .msix, optionally signing it.

.EXAMPLE
  dotnet publish Updraft/Updraft.csproj -c Release -p:Platform=x64 -r win-x64 --self-contained true -o publish/win-x64
  ./packaging/build-msix.ps1 -PublishDir publish/win-x64 -Arch x64 -Version 0.2.0 -OutFile Updraft-win-x64.msix

.NOTES
  Unsigned output is what you upload to the Microsoft Store (Microsoft signs it for you).
  For Store submission, pass the Identity Name and Publisher from Partner Center > Product identity.
  Pass -PfxPath/-PfxPassword to produce a signed package for sideloading.
#>
param(
    [Parameter(Mandatory)][string]$PublishDir,
    [Parameter(Mandatory)][ValidateSet('x64', 'arm64')][string]$Arch,
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][string]$OutFile,
    [string]$IdentityName = 'BilalAnsari.Updraft',
    [string]$Publisher = 'CN=Bilal Ansari',
    [string]$PublisherDisplayName = 'Bilal Ansari',
    [string]$PfxPath,
    [string]$PfxPassword
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\SdkTool.ps1"

if (-not (Test-Path (Join-Path $PublishDir 'Updraft.exe'))) { throw "No Updraft.exe in $PublishDir" }

# MSIX versions are exactly four numeric parts: 0.2.0-beta1 -> 0.2.0.0
$nums = @(($Version -replace '[-+].*', '').Split('.') | ForEach-Object { [int]$_ })
while ($nums.Count -lt 4) { $nums += 0 }
$msixVersion = ($nums[0..3]) -join '.'

$stage = Join-Path ([IO.Path]::GetTempPath()) "updraft-msix-$Arch"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory $stage | Out-Null

Copy-Item (Join-Path $PublishDir '*') $stage -Recurse -Exclude '*.pdb'
# The publish output already has an Assets folder (app icon), so copy the logos INTO it rather than as a subfolder.
$assets = Join-Path $stage 'Assets'
New-Item -ItemType Directory $assets -Force | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'msix\Assets\*') $assets -Force

$esc = { param($s) [System.Security.SecurityElement]::Escape($s) }
$manifest = Get-Content (Join-Path $PSScriptRoot 'msix\AppxManifest.template.xml') -Raw
$manifest = $manifest.Replace('@IDENTITY_NAME@', (& $esc $IdentityName)).
    Replace('@PUBLISHER@', (& $esc $Publisher)).
    Replace('@PUBLISHER_DISPLAY@', (& $esc $PublisherDisplayName)).
    Replace('@VERSION@', $msixVersion).
    Replace('@ARCH@', $Arch)
[IO.File]::WriteAllText((Join-Path $stage 'AppxManifest.xml'), $manifest, (New-Object Text.UTF8Encoding $false))

$out = [IO.Path]::GetFullPath($OutFile)
if (Test-Path $out) { Remove-Item $out -Force }
Invoke-Tool (Find-SdkTool 'makeappx.exe') @('pack', '/d', $stage, '/p', $out, '/o')

Sign-Package $out $PfxPath $PfxPassword
Remove-Item $stage -Recurse -Force

Write-Host "Created $out ($msixVersion, $Arch)$(if ($PfxPath) { ', signed' } else { ', UNSIGNED (Store upload only)' })"
