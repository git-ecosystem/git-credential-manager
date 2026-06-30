<#
.SYNOPSIS
    Builds the Windows distributables for Git Credential Manager.

.DESCRIPTION
    Runs the publish, pack and archive steps in sequence to produce the system
    and user installer packages (.exe) together with the binary and symbol
    archives (.zip), under the top-level artifacts package directory.

.PARAMETER InnoSetup
    Path to the Inno Setup compiler (ISCC.exe). MSBuild callers pass the
    Tools.InnoSetup package's $(InnoSetupCompiler); when running this script
    directly, either put iscc on PATH or pass this explicitly.
#>
[CmdletBinding()]
param (
    [Alias('c')] [string] $Configuration,
    [string] $Version,
    [Alias('r')] [string] $Runtime,
    [string] $InnoSetup
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'

Import-Module "$PSScriptRoot/../lib-cli.psm1" -Force

# Normalise arguments / apply defaults.
$Configuration = Resolve-Configuration $Configuration
$Runtime = Resolve-Runtime $Runtime
$Version = Resolve-Version $Version

# Thread the verbose preference through to the child scripts.
$ChildVerbose = ($VerbosePreference -eq 'Continue')

# Invoke a child build script and propagate a non-zero exit code.
function Invoke-Step {
    param(
        [Parameter(Mandatory)] [string] $Script,
        [Parameter(Mandatory)] [hashtable] $Arguments
    )
    & (Join-Path $PSScriptRoot $Script) @Arguments -Verbose:$ChildVerbose
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

Write-Information "Building Windows distribution..."
Write-Verbose "configuration: $Configuration"
Write-Verbose "runtime:       $Runtime"
Write-Verbose "version:       $Version"

# Publish the application.
Invoke-Step 'publish.ps1' @{ Configuration = $Configuration; Runtime = $Runtime; Version = $Version }

# Build the installer packages (.exe).
Invoke-Step 'pack.ps1' @{ Configuration = $Configuration; Runtime = $Runtime; InnoSetup = $InnoSetup }

# Create the binary and symbol archives (.zip).
Invoke-Step 'archive.ps1' @{ Configuration = $Configuration; Runtime = $Runtime; Version = $Version }

Write-Information "Windows distribution build complete."
