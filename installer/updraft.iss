; Inno Setup script for Updraft.
; Build:  ISCC /DVersion=0.1.0 /DArch=x64 /DSourceDir=..\publish\win-x64 /DOutputDir=..\dist updraft.iss
;         ISCC /DVersion=0.1.0 /DArch=arm64 /DSourceDir=..\publish\win-arm64 /DOutputDir=..\dist updraft.iss

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
AppName=Updraft
AppVersion={#Version}
AppPublisher=Bilal Ansari
AppPublisherURL=https://github.com/bilal8492/updraft
AppSupportURL=https://github.com/bilal8492/updraft/issues
AppUpdatesURL=https://github.com/bilal8492/updraft/releases
VersionInfoVersion={#Version}
DefaultDirName={autopf}\Updraft
DefaultGroupName=Updraft
DisableProgramGroupPage=yes
; Installs for the current user by default (no admin prompt); the user can choose "all users" in the wizard.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir={#OutputDir}
OutputBaseFilename=Updraft-Setup-{#Arch}
SetupIconFile=..\Updraft\Assets\updraft.ico
UninstallDisplayIcon={app}\Updraft.exe
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

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{autoprograms}\Updraft"; Filename: "{app}\Updraft.exe"
Name: "{autodesktop}\Updraft"; Filename: "{app}\Updraft.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Updraft.exe"; Description: "Launch Updraft"; Flags: nowait postinstall skipifsilent
