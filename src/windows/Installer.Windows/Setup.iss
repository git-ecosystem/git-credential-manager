; This script requires Inno Setup Compiler 5.6.1 or later to compile
; The Inno Setup Compiler (and IDE) can be found at http://www.jrsoftware.org/isinfo.php
; General documentation on how to use InnoSetup scripts: http://www.jrsoftware.org/ishelp/index.php

; Ensure minimum Inno Setup tooling version
#if VER < EncodeVer(5,6,1)
  #error Update your Inno Setup version (5.6.1 or newer)
#endif

#ifndef PayloadDir
  #error Payload directory path property 'PayloadDir' must be specified
#endif

#ifnexist PayloadDir + "\git-credential-manager-core.exe"
  #error Payload files are missing
#endif

; Define core properties
#define GcmName "Git Credential Manager Core"
#define GcmPublisher "Microsoft Corporation"
#define GcmPublisherUrl "https://www.microsoft.com"
#define GcmCopyright "Copyright (c) Microsoft 2019"
#define GcmUrl "https://github.com/microsoft/Git-Credential-Manager-Core"
#define GcmReadme "https://github.com/microsoft/Git-Credential-Manager-Core/blob/master/README.md"
#define GcmRepoRoot "..\..\.."
#define GcmAssets GcmRepoRoot + "\assets"

; Generate the GCM version version from the CLI executable
#define VerMajor
#define VerMinor
#define VerBuild
#define VerRevision
#expr ParseVersion(PayloadDir + "\git-credential-manager-core.exe", VerMajor, VerMinor, VerBuild, VerRevision)
#define GcmVersion str(VerMajor) + "." + str(VerMinor) + "." + str(VerBuild) + "." + str(VerRevision)

[Setup]
AppId={{fdfae50a-1bc1-4ead-9228-1e1c275e8d12}}
AppName={#GcmName}
AppVersion={#GcmVersion}
AppVerName={#GcmName} {#GcmVersion}
AppPublisher={#GcmPublisher}
AppPublisherURL={#GcmPublisherUrl}
AppSupportURL={#GcmUrl}
AppUpdatesURL={#GcmUrl}
AppContact={#GcmUrl}
AppCopyright={#GcmCopyright}
AppReadmeFile={#GcmReadme}
VersionInfoVersion={#GcmVersion}
LicenseFile={#GcmRepoRoot}\LICENSE
OutputBaseFilename=gcmcore-windows-x86-{#GcmVersion}
DefaultDirName={pf}\{#GcmName}
Compression=lzma2
SolidCompression=yes
MinVersion=6.1.7600
DisableDirPage=yes
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\git-credential-manager-core.exe
SetupIconFile={#GcmAssets}\gcmicon.ico
WizardImageFile={#GcmAssets}\gcmicon128.bmp
WizardSmallImageFile={#GcmAssets}\gcmicon64.bmp
WizardStyle=modern
WizardImageStretch=no
WindowResizable=no
ChangesEnvironment=yes

[Languages]
Name: english; MessagesFile: "compiler:Default.isl";

[Types]
Name: full; Description: "Full installation"; Flags: iscustom;

[Components]
; No individual components

[Run]
Filename: "{app}\git-credential-manager-core.exe"; Parameters: "configure"; Flags: runhidden

[UninstallRun]
Filename: "{app}\git-credential-manager-core.exe"; Parameters: "unconfigure"; Flags: runhidden

[Files]
Source: "{#PayloadDir}\git-credential-manager-core.exe";               DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\GitHub.dll";                                    DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\GitHub.UI.dll";                                 DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\GitHub.Authentication.Helper.exe";              DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\git2-572e4d8.dll";                              DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\Microsoft.Authentication.Helper.exe";           DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\Microsoft.Authentication.Helper.exe.config";    DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\Microsoft.AzureRepos.dll";                      DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\Microsoft.Git.CredentialManager.dll";           DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\Microsoft.Identity.Client.dll";                 DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\Microsoft.Identity.Client.Extensions.Msal.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\Microsoft.IdentityModel.JsonWebTokens.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\Microsoft.IdentityModel.Logging.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\Microsoft.IdentityModel.Tokens.dll"; DestDir: "{app}"; Flags: ignoreversion
