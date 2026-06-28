<#
.SYNOPSIS
    Archives the published Git Credential Manager application into .zip files.

.DESCRIPTION
    Creates distributable archives of the published application: a payload
    archive of the shipping binaries and a separate symbols archive, written to
    the top-level artifacts package directory.
#>
[CmdletBinding()]
param (
    [Alias('c')] [string] $Configuration,
    [Alias('r')] [string] $Runtime,
    [string] $Version,
    [string] $BinDir,
    [string] $SymbolDir,
    [string] $Output
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'

Import-Module "$PSScriptRoot/../lib-cli.psm1" -Force

# Normalise arguments / apply defaults.
$Configuration = Resolve-Configuration $Configuration
$Runtime = Resolve-Runtime $Runtime
$Version = Resolve-Version $Version

# Source of the published binaries to archive (override with -BinDir; defaults to
# the application's default artifacts publish directory, from publish.ps1).
if ($BinDir) {
    $BinDir = Get-AbsolutePath $BinDir
} else {
    $BinDir = Get-PublishDir -Project 'git-credential-manager' -Configuration $Configuration -Runtime $Runtime
}

# Source of the debug symbols to archive (override with -SymbolDir; defaults to
# the sibling symbol directory produced by publish.ps1).
if ($SymbolDir) {
    $SymbolDir = Get-AbsolutePath $SymbolDir
} else {
    $SymbolDir = "$BinDir.sym"
}

# Destination directory for the archives.
if ($Output) {
    $OutDir = Get-AbsolutePath $Output
} else {
    $OutDir = Get-PackageDir -Configuration $Configuration
}

$PayloadZip = Join-Path $OutDir "gcm-$Runtime-$Version.zip"
$SymbolsZip = Join-Path $OutDir "gcm-$Runtime-$Version-symbols.zip"

Write-Verbose "configuration: $Configuration"
Write-Verbose "runtime:       $Runtime"
Write-Verbose "version:       $Version"
Write-Verbose "bin dir:       $BinDir"
Write-Verbose "symbol dir:    $SymbolDir"
Write-Verbose "output dir:    $OutDir"

# Pre-execution checks
if (-not (Test-Path -LiteralPath $BinDir)) {
    Write-Error "Binaries directory '$BinDir' not found. Did you publish first?"
}

New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

# Archive the shipping binaries.
Write-Information "Archiving binaries from '$BinDir'..."
if (Test-Path -LiteralPath $PayloadZip) { Remove-Item -Force -LiteralPath $PayloadZip }
Compress-Archive -Path "$BinDir/*" -DestinationPath $PayloadZip
Write-Information "Created $PayloadZip"

# Archive the debug symbols, if any were produced.
if ((Test-Path -LiteralPath $SymbolDir) -and (Get-ChildItem -LiteralPath $SymbolDir -Force)) {
    Write-Information "Archiving symbols from '$SymbolDir'..."
    if (Test-Path -LiteralPath $SymbolsZip) { Remove-Item -Force -LiteralPath $SymbolsZip }
    Compress-Archive -Path "$SymbolDir/*" -DestinationPath $SymbolsZip
    Write-Information "Created $SymbolsZip"
} else {
    Write-Warning "symbols directory '$SymbolDir' not found or empty; skipping symbols archive"
}

Write-Information "Archiving complete."
