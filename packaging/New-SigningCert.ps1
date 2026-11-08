<#
.SYNOPSIS
  Creates the self-signed certificate used to sign the sideload .msix, once.

.DESCRIPTION
  Writes UpInstall-signing.pfx (private key: keep secret) and UpInstall-signing.cer (public: users trust this).
  Add the PFX to the GitHub repo as secrets so CI signs every release with the SAME certificate:
    MSIX_PFX_BASE64     = the base64 text this script prints
    MSIX_PFX_PASSWORD   = the password you entered
  The certificate Subject must equal the manifest Publisher (default 'CN=Bilal Ansari').
  Do NOT commit the .pfx. The .gitignore blocks it.
#>
param(
    [string]$Subject = 'CN=Bilal Ansari',
    [string]$OutDir = (Get-Location).Path,
    [int]$Years = 5
)

$ErrorActionPreference = 'Stop'
$password = Read-Host 'Choose a password for the PFX' -AsSecureString

$cert = New-SelfSignedCertificate -Type Custom -Subject $Subject -KeyUsage DigitalSignature `
    -FriendlyName 'UpInstall sideload signing' -CertStoreLocation 'Cert:\CurrentUser\My' `
    -NotAfter (Get-Date).AddYears($Years) `
    -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3', '2.5.29.19={text}')   # code signing, not a CA

try {
    $pfx = Join-Path $OutDir 'UpInstall-signing.pfx'
    $cer = Join-Path $OutDir 'UpInstall-signing.cer'
    Export-PfxCertificate -Cert $cert -FilePath $pfx -Password $password | Out-Null
    Export-Certificate -Cert $cert -FilePath $cer | Out-Null
}
finally {
    Remove-Item "Cert:\CurrentUser\My\$($cert.Thumbprint)" -Force   # don't leave the private key in your cert store
}

Write-Host "Created: $pfx"
Write-Host "Created: $cer"
Write-Host ''
Write-Host 'Base64 for the MSIX_PFX_BASE64 secret (copied to clipboard):'
[Convert]::ToBase64String([IO.File]::ReadAllBytes($pfx)) | Set-Clipboard
Write-Host '  (paste it into GitHub > Settings > Secrets and variables > Actions)'
