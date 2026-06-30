<#
.SYNOPSIS
    Builds the Windows installer packages (.exe) for Git Credential Manager.

.DESCRIPTION
    Compiles the Inno Setup script (installer/Setup.iss) with the Inno Setup
    command-line compiler (ISCC) to produce the system and user installer
    executables. The installer version is derived by Inno Setup from the
    published git-credential-manager.exe itself.
#>
[CmdletBinding()]
param (
    [Alias('c')] [string] $Configuration,
    [Alias('r')] [string] $Runtime,
    [string] $BinDir,
    [string] $Output,
    # Path to the Inno Setup compiler (ISCC.exe). MSBuild callers pass the
    # Tools.InnoSetup package's $(InnoSetupCompiler); direct callers may omit
    # this if iscc is on PATH.
    [string] $InnoSetup
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'

Import-Module "$PSScriptRoot/../lib-cli.psm1" -Force

# Normalise arguments / apply defaults.
$Configuration = Resolve-Configuration $Configuration
$Runtime = Resolve-Runtime $Runtime
$Iscc = Resolve-InnoSetup $InnoSetup

# The Inno Setup script lives in the sibling installer/ directory.
$SetupIss = Join-Path $PSScriptRoot 'installer' 'Setup.iss'

# Source of the published binaries to package (override with -BinDir; defaults to
# the application's default artifacts publish directory, from publish.ps1).
if ($BinDir) {
    $BinDir = Get-AbsolutePath $BinDir
} else {
    $BinDir = Get-PublishDir -Project 'git-credential-manager' -Configuration $Configuration -Runtime $Runtime
}

# Destination directory for the packages (override with -Output; defaults to the
# top-level artifacts package directory). Inno Setup defines each file name.
if ($Output) {
    $OutDir = Get-AbsolutePath $Output
} else {
    $OutDir = Get-PackageDir -Configuration $Configuration
}

Write-Verbose "configuration: $Configuration"
Write-Verbose "runtime:       $Runtime"
Write-Verbose "iscc:          $Iscc"
Write-Verbose "bin dir:       $BinDir"
Write-Verbose "output dir:    $OutDir"

# Pre-execution checks
if (-not (Test-Path -LiteralPath $BinDir)) {
    Write-Error "Payload directory '$BinDir' not found. Did you publish first?"
}
if (-not (Test-Path -LiteralPath $SetupIss)) {
    Write-Error "Inno Setup script '$SetupIss' not found"
}

New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

# Compile the Inno Setup script for a given install target ('system' or 'user').
function Build-Installer {
    param([Parameter(Mandatory)] [string] $InstallTarget)

    Write-Information "Building $InstallTarget installer package..."
    & $Iscc `
        "/O$OutDir" `
        "/DPayloadDir=$BinDir" `
        "/DInstallTarget=$InstallTarget" `
        "/DRuntimeIdentifier=$Runtime" `
        $SetupIss
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Inno Setup failed for the $InstallTarget installer (ISCC exited $LASTEXITCODE)"
    }
}

Build-Installer -InstallTarget 'system'
Build-Installer -InstallTarget 'user'

Write-Information "Packaging complete: $OutDir"
