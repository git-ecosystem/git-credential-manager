; This script requires Inno Setup Compiler 6.0.0 or later to compile
; The Inno Setup Compiler (and IDE) can be found at http://www.jrsoftware.org/isinfo.php
; General documentation on how to use InnoSetup scripts: http://www.jrsoftware.org/ishelp/index.php

; Ensure minimum Inno Setup tooling version
#if VER < EncodeVer(6,0,0)
  #error Update your Inno Setup version (6.0.0 or newer)
#endif

#ifndef PayloadDir
  #error Payload directory path property 'PayloadDir' must be specified
#endif

#ifndef InstallTarget
  #error Installer target property 'InstallTarget' must be specifed
#endif

#if InstallTarget == "user"
  #define GcmAppId "{{aa76d31d-432c-42ee-844c-bc0bc801cef3}}"
  #define GcmLongName "Git Credential Manager Core (User)"
  #define GcmSetupExe "gcmcoreuser"
  #define GcmConfigureCmdArgs "--user"
#elif InstallTarget == "system"
  #define GcmAppId "{{fdfae50a-1bc1-4ead-9228-1e1c275e8d12}}"
  #define GcmLongName "Git Credential Manager Core"
  #define GcmSetupExe "gcmcore"
  #define GcmConfigureCmdArgs "--system"
#else
  #error Installer target property 'InstallTarget' must be 'user' or 'system'
#endif

; Define core properties
#define GcmShortName "Git Credential Manager Core"
#define GcmPublisher "Microsoft Corporation"
#define GcmPublisherUrl "https://www.microsoft.com"
#define GcmCopyright "Copyright (c) Microsoft 2020"
#define GcmUrl "https://aka.ms/gcmcore"
#define GcmReadme "https://github.com/microsoft/Git-Credential-Manager-Core/blob/master/README.md"
#define GcmRepoRoot "..\..\.."
#define GcmAssets GcmRepoRoot + "\assets"
#define GcmExe "git-credential-manager-core.exe"
#define GcmArch "x86"

#ifnexist PayloadDir + "\" + GcmExe
  #error Payload files are missing
#endif

; Generate the GCM version version from the CLI executable
#define VerMajor
#define VerMinor
#define VerBuild
#define VerRevision
#expr ParseVersion(PayloadDir + "\" + GcmExe, VerMajor, VerMinor, VerBuild, VerRevision)
#define GcmVersion str(VerMajor) + "." + str(VerMinor) + "." + str(VerBuild) + "." + str(VerRevision)

[Setup]
AppId={#GcmAppId}
AppName={#GcmLongName}
AppVersion={#GcmVersion}
AppVerName={#GcmLongName} {#GcmVersion}
AppPublisher={#GcmPublisher}
AppPublisherURL={#GcmPublisherUrl}
AppSupportURL={#GcmUrl}
AppUpdatesURL={#GcmUrl}
AppContact={#GcmUrl}
AppCopyright={#GcmCopyright}
AppReadmeFile={#GcmReadme}
VersionInfoVersion={#GcmVersion}
LicenseFile={#GcmRepoRoot}\LICENSE
OutputBaseFilename={#GcmSetupExe}-win-{#GcmArch}-{#GcmVersion}
DefaultDirName={autopf}\{#GcmShortName}
Compression=lzma2
SolidCompression=yes
MinVersion=6.1.7600
DisableDirPage=yes
UninstallDisplayIcon={app}\{#GcmExe}
SetupIconFile={#GcmAssets}\gcmicon.ico
WizardImageFile={#GcmAssets}\gcmicon128.bmp
WizardSmallImageFile={#GcmAssets}\gcmicon64.bmp
WizardStyle=modern
WizardImageStretch=no
WindowResizable=no
ChangesEnvironment=yes
#if InstallTarget == "user"
  PrivilegesRequired=lowest
#endif

[Languages]
Name: english; MessagesFile: "compiler:Default.isl";

[Types]
Name: full; Description: "Full installation"; Flags: iscustom;

[Components]
; No individual components

[Run]
Filename: "{app}\{#GcmExe}"; Parameters: "configure {#GcmConfigureCmdArgs}"; Flags: runhidden

[UninstallRun]
Filename: "{app}\{#GcmExe}"; Parameters: "unconfigure {#GcmConfigureCmdArgs}"; Flags: runhidden

[Files]
Source: "{#PayloadDir}\Atlassian.Bitbucket.dll";                       DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\Atlassian.Bitbucket.UI.exe";                    DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\Atlassian.Bitbucket.UI.exe.config";             DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\git-credential-manager-core.exe";               DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\git-credential-manager-core.exe.config";        DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\GitHub.dll";                                    DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\GitHub.UI.exe";                                 DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\GitHub.UI.exe.config";                          DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\Microsoft.AzureRepos.dll";                      DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\Microsoft.Git.CredentialManager.dll";           DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\Microsoft.Git.CredentialManager.UI.dll";        DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\Microsoft.Identity.Client.dll";                 DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\Microsoft.Identity.Client.Extensions.Msal.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\Newtonsoft.Json.dll";                           DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\System.Buffers.dll";                            DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\System.CommandLine.dll";                        DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\System.Memory.dll";                             DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\System.Numerics.Vectors.dll";                   DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\System.Runtime.CompilerServices.Unsafe.dll";    DestDir: "{app}"; Flags: ignoreversion

[Code]
// Don't allow installing conflicting architectures
function InitializeSetup(): Boolean;
begin
  Result := True;

  #if InstallTarget == "user"
    if not WizardSilent() and IsAdmin() then begin
      if MsgBox('This User Installer is not meant to be run as an Administrator. If you would like to install Git Credential Manager Core for all users in this system, download the System Installer instead from https://aka.ms/gcmcore-latest. Are you sure you want to continue?', mbError, MB_OKCANCEL) = IDCANCEL then begin
        Result := False;
      end;
    end;
  #endif
end;
