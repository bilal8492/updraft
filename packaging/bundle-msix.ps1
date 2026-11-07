<#
.SYNOPSIS
  Combines the per-architecture .msix files in a folder into one .msixbundle (what you upload to the Store).
#>
param(
    [Parameter(Mandatory)][string]$InputDir,
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][string]$OutFile,
    [string]$PfxPath,
    [string]$PfxPassword
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\SdkTool.ps1"

$nums = @(($Version -replace '[-+].*', '').Split('.') | ForEach-Object { [int]$_ })
while ($nums.Count -lt 4) { $nums += 0 }
$bundleVersion = ($nums[0..3]) -join '.'

# makeappx bundles every .msix in the folder, so copy just the ones we want into a clean one.
$stage = Join-Path ([IO.Path]::GetTempPath()) 'updraft-msixbundle'
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory $stage | Out-Null
$files = Get-ChildItem $InputDir -Filter *.msix
if ($files.Count -eq 0) { throw "No .msix files in $InputDir" }
$files | Copy-Item -Destination $stage

$out = [IO.Path]::GetFullPath($OutFile)
if (Test-Path $out) { Remove-Item $out -Force }
Invoke-Tool (Find-SdkTool 'makeappx.exe') @('bundle', '/d', $stage, '/bv', $bundleVersion, '/p', $out, '/o')

Sign-Package $out $PfxPath $PfxPassword
Remove-Item $stage -Recurse -Force
Write-Host "Created $out ($bundleVersion) from $($files.Count) package(s)"
