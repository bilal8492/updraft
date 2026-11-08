# Packaging UpInstall as MSIX

UpInstall ships three ways: the `-setup.exe` installer and portable `.zip` (see the main README) and an
**MSIX**, which gives clean install/uninstall, automatic per-user updates through the Microsoft Store, and no
SmartScreen warning when it comes from the Store.

The MSIX wraps the same self-contained build as the zip, so there is no second code path to maintain.

| File | Purpose |
| --- | --- |
| `msix/AppxManifest.template.xml` | Package manifest; the script fills in identity, version and architecture. |
| `msix/Assets/` | Tile and Store logos, generated from `UpInstall/Assets/upinstall.png`. |
| `build-msix.ps1` | Packs one architecture into an `.msix`, optionally signing it. |
| `bundle-msix.ps1` | Combines the x64 and ARM64 `.msix` files into one `.msixbundle`. |
| `New-SigningCert.ps1` | Creates the self-signed certificate for the sideload package (run once). |

## Build a package locally

```powershell
dotnet publish UpInstall/UpInstall.csproj -c Release -p:Platform=x64 -r win-x64 --self-contained true -o publish/win-x64
./packaging/build-msix.ps1 -PublishDir publish/win-x64 -Arch x64 -Version 0.2.0 -OutFile UpInstall-win-x64.msix
```

`makeappx` and `signtool` come from the `Microsoft.Windows.SDK.BuildTools` NuGet package the project already
restores, so no extra install is needed.

## Sideload package from GitHub releases (self-signed)

A self-signed package is not trusted by Windows until the user trusts our certificate, so this is for people who
want the MSIX outside the Store. The `.exe` installer remains the easier choice for most users.

### One-time setup (maintainer)

1. Run `./packaging/New-SigningCert.ps1`. It writes `UpInstall-signing.pfx` and `UpInstall-signing.cer` and copies the
   PFX as base64 to your clipboard. **Never commit the `.pfx`** (`.gitignore` blocks it) and keep a backup: losing it
   means users must trust a new certificate.
2. In the GitHub repo, add two Actions secrets: `MSIX_PFX_BASE64` (paste) and `MSIX_PFX_PASSWORD`.
3. Push a `v*` tag as usual. The release now also contains `UpInstall.msixbundle` and `UpInstall-signing.cer`.
   Without the secrets the MSIX steps are skipped and the release is unchanged.

The certificate Subject must equal the manifest Publisher (`CN=Bilal Ansari` by default).

### Install (user)

Trusting the certificate is an administrator action. In an elevated PowerShell, in the download folder:

```powershell
Import-Certificate -FilePath .\UpInstall-signing.cer -CertStoreLocation Cert:\LocalMachine\TrustedPeople
Add-AppxPackage .\UpInstall.msixbundle
```

Only do this if you trust this project: the certificate lets its owner sign packages your PC will accept.
Remove it later in `certlm.msc` under Trusted People.

## Microsoft Store

Microsoft re-signs MSIX packages submitted to the Store for free, so you upload an **unsigned** package built with
your Partner Center identity:

1. In Partner Center, reserve the name and open **Product identity**. Note the *Package/Identity/Name*,
   *Package/Identity/Publisher* (looks like `CN=GUID`) and *PublisherDisplayName*.
2. Build both architectures with those values, then bundle:

   ```powershell
   ./packaging/build-msix.ps1 -PublishDir publish/win-x64   -Arch x64   -Version 0.2.0 -OutFile store/UpInstall-win-x64.msix   -IdentityName '<Name>' -Publisher '<Publisher>' -PublisherDisplayName '<Display>'
   ./packaging/build-msix.ps1 -PublishDir publish/win-arm64 -Arch arm64 -Version 0.2.0 -OutFile store/UpInstall-win-arm64.msix -IdentityName '<Name>' -Publisher '<Publisher>' -PublisherDisplayName '<Display>'
   ./packaging/bundle-msix.ps1 -InputDir store -Version 0.2.0 -OutFile UpInstall.msixbundle
   ```

3. Upload `UpInstall.msixbundle` in the submission's Packages step.

Store notes: the app uses the `runFullTrust` capability (it launches winget, PowerShell and Windows Terminal), which is
normal for desktop apps. Each submission needs a higher version than the last. A privacy policy URL is required;
the website can host one (UpInstall collects nothing).

## Behaviour differences inside MSIX

- Settings (`%LOCALAPPDATA%\UpInstall\settings.json`) are stored in the package's private copy of AppData and are
  removed on uninstall. That is intended.
- "Run in terminal" sends its script on the command line (`-EncodedCommand`) instead of writing a file, because files
  written under AppData are invisible to Windows Terminal and to an elevated PowerShell when running as MSIX.
