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

Code signing is free for the Store: Microsoft re-signs MSIX packages after certification (see
[Code signing options](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options)), so the
Store package is deliberately **unsigned** and built with the Partner Center identity:

| Field | Value |
| --- | --- |
| Package/Identity/Name | `BilalAnsari.UpInstall` |
| Package/Identity/Publisher | `CN=1C7A51E9-3ED2-476B-B6C8-5086736F112C` |
| PublisherDisplayName | `Bilal Ansari` |
| Store ID | `9PMB0883K391` |

(These differ from the sideload package, which keeps the old `BilalAnsari.Updraft` identity.)

### First submission (manual, once)

The Store API can only update an app that already has a published submission, so do the first one in Partner Center:
build the bundle (workflow artifact `store-bundle`, or locally as below), upload `UpInstall.msixbundle` under
**Packages**, fill in Properties, Age ratings, Pricing and availability and the Store listing, then submit.
A privacy policy URL is required; the website can host one (UpInstall collects nothing).

```powershell
./packaging/build-msix.ps1 -PublishDir publish/win-x64   -Arch x64   -Version 0.2.0 -OutFile store/UpInstall-win-x64.msix   -IdentityName 'BilalAnsari.UpInstall' -Publisher 'CN=1C7A51E9-3ED2-476B-B6C8-5086736F112C' -PublisherDisplayName 'Bilal Ansari'
./packaging/build-msix.ps1 -PublishDir publish/win-arm64 -Arch arm64 -Version 0.2.0 -OutFile store/UpInstall-win-arm64.msix -IdentityName 'BilalAnsari.UpInstall' -Publisher 'CN=1C7A51E9-3ED2-476B-B6C8-5086736F112C' -PublisherDisplayName 'Bilal Ansari'
./packaging/bundle-msix.ps1 -InputDir store -Version 0.2.0 -OutFile UpInstall.msixbundle
```

### Automatic submissions from releases

The `store` job in `.github/workflows/release.yml` bundles the unsigned packages and, when the secrets below exist,
submits them with the [Store Developer CLI](https://learn.microsoft.com/en-us/windows/apps/publish/msstore-dev-cli/ci-cd-environments)
(`msstore publish`). Pre-release tags (containing `-`) are never submitted. Without the secrets the bundle is only
kept as the `store-bundle` artifact.

One-time setup:

1. Partner Center > **Account settings** > **Tenant administration** > **Microsoft Entra applications**: add (or
   create) an application, give it the **Manager** role, and create a client secret.
2. In the GitHub repo add these Actions secrets: `PARTNER_CENTER_TENANT_ID`, `PARTNER_CENTER_SELLER_ID`,
   `PARTNER_CENTER_CLIENT_ID`, `PARTNER_CENTER_CLIENT_SECRET`.
3. Optional repository variables (defaults are the values above): `STORE_PRODUCT_ID`, `STORE_IDENTITY_NAME`,
   `STORE_PUBLISHER`, `STORE_PUBLISHER_DISPLAY_NAME`.

Store notes: the app uses the `runFullTrust` capability (it launches winget, PowerShell and Windows Terminal), which is
normal for desktop apps. Each submission needs a higher version than the last, and the Store reserves the fourth part
of the version, which is why releases map `1.2.3` to `1.2.3.0`.

## Behaviour differences inside MSIX

- Settings (`%LOCALAPPDATA%\UpInstall\settings.json`) are stored in the package's private copy of AppData and are
  removed on uninstall. That is intended.
- "Run in terminal" sends its script on the command line (`-EncodedCommand`) instead of writing a file, because files
  written under AppData are invisible to Windows Terminal and to an elevated PowerShell when running as MSIX.
