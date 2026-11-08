; Inno Setup script for UpInstall.
; Build:  ISCC /DVersion=0.1.0 /DArch=x64 /DSourceDir=..\publish\win-x64 /DOutputDir=..\dist upinstall.iss
;         ISCC /DVersion=0.1.0 /DArch=arm64 /DSourceDir=..\publish\win-arm64 /DOutputDir=..\dist upinstall.iss

#ifndef Version
  #define Version "0.0.0"
#endif
#ifndef Arch
  #define Arch "x64"
#endif
#ifndef SourceDir
  #define SourceDir "..\publish\win-x64"
#endif
#ifndef OutputDir
  #define OutputDir "..\dist"
#endif

[Setup]
AppId={{B6C1A8E2-4D3F-4B7A-9E55-7A1F0C2D9E41}
AppName=UpInstall
AppVersion={#Version}
AppPublisher=Bilal Ansari
AppPublisherURL=https://github.com/bilal8492/upinstall
AppSupportURL=https://github.com/bilal8492/upinstall/issues
AppUpdatesURL=https://github.com/bilal8492/upinstall/releases
VersionInfoVersion={#Version}
DefaultDirName={autopf}\UpInstall
DefaultGroupName=UpInstall
DisableProgramGroupPage=yes
; Installs for the current user by default (no admin prompt); the user can choose "all users" in the wizard.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir={#OutputDir}
OutputBaseFilename=UpInstall-Setup-{#Arch}
SetupIconFile=..\UpInstall\Assets\upinstall.ico
UninstallDisplayIcon={app}\UpInstall.exe
LicenseFile=..\LICENSE
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
#if Arch == "arm64"
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#else
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#endif
CloseApplications=yes

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; Flags: unchecked

; The app was called Updraft before v0.6. AppId above is unchanged, so this upgrades those installs in place;
; clear out the old-named program files and shortcuts so only UpInstall is left.
[InstallDelete]
Type: files; Name: "{app}\Updraft.*"
Type: files; Name: "{autoprograms}\Updraft.lnk"
Type: files; Name: "{autodesktop}\Updraft.lnk"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{autoprograms}\UpInstall"; Filename: "{app}\UpInstall.exe"
Name: "{autodesktop}\UpInstall"; Filename: "{app}\UpInstall.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\UpInstall.exe"; Description: "Launch UpInstall"; Flags: nowait postinstall skipifsilent
