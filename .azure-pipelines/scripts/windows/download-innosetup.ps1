<#
.SYNOPSIS
    Downloads the Inno Setup compiler (ISCC.exe) used to build the Windows
    installers, without requiring a project file.

.DESCRIPTION
    Uses 'dotnet package download' (.NET SDK 10+) to fetch the Tools.InnoSetup
    NuGet package into a local folder, then returns an object describing the
    download. By default the version pinned for the build in
    Directory.Packages.props is used, so the installers are compiled with the
    same Inno Setup as a regular 'dotnet build'.

    The returned object lets the caller either pass the compiler path to
    pack.ps1 (-InnoSetup) or add the tools directory to PATH (e.g. via an Azure
    Pipelines '##vso[task.prependpath]' command); this script itself stays
    agnostic of the build/CI system.

.PARAMETER Version
    The Tools.InnoSetup package version to download. Defaults to the version
    pinned in Directory.Packages.props.

.PARAMETER OutputPath
    Directory to download the package into. Defaults to out/tools/innosetup under
    the repository root.

.OUTPUTS
    A [pscustomobject] with these properties:
      Version  - the resolved Tools.InnoSetup package version.
      ToolsDir - the directory containing the Inno Setup binaries (ISCC.exe etc.).
      Path     - the full path to ISCC.exe.
#>
[CmdletBinding()]
param (
    [string] $Version,
    [string] $OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'

# Repository root, three levels up from this script
# (.azure-pipelines/scripts/windows).
function Get-RepoRoot {
    return (Get-Item $PSScriptRoot).Parent.Parent.Parent.FullName
}

function Get-AbsolutePath {
    param([Parameter(Mandatory)] [string] $Path)
    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }
    return (Join-Path (Get-Location).Path $Path)
}

# Resolve the package version (default: the version pinned for the build in
# Directory.Packages.props, so installers use the same Inno Setup as the build).
if (-not $Version) {
    $propsPath = Join-Path (Get-RepoRoot) 'Directory.Packages.props'
    if (-not (Test-Path -LiteralPath $propsPath)) {
        Write-Error "Central package versions file '$propsPath' not found"
    }
    [xml]$props = Get-Content -LiteralPath $propsPath
    $Version = ($props.Project.ItemGroup.PackageVersion |
        Where-Object { $_.Include -eq 'Tools.InnoSetup' }).Version
    if (-not $Version) {
        Write-Error "Tools.InnoSetup version not found in '$propsPath'"
    }
}

# Resolve the download directory (default: out/tools/innosetup).
if ($OutputPath) {
    $OutputPath = Get-AbsolutePath $OutputPath
} else {
    $OutputPath = Join-Path (Get-RepoRoot) 'out' 'tools' 'innosetup'
}

Write-Information "Downloading Inno Setup $Version to '$OutputPath'..."

# Display the download output on the host without letting it leak into this
# script's output stream (so the returned object is the only success output).
dotnet package download "Tools.InnoSetup@$Version" --output $OutputPath | Out-Host
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet package download failed (exit $LASTEXITCODE)"
}

# 'dotnet package download' extracts each package to
# <output>/<id-lowercased>/<version>/; the Inno Setup binaries live under tools/.
$toolsDir = Join-Path $OutputPath 'tools.innosetup' $Version 'tools'
$iscc = Join-Path $toolsDir 'ISCC.exe'
if (-not (Test-Path -LiteralPath $iscc)) {
    Write-Error "ISCC.exe not found at '$iscc'"
}

Write-Information "Inno Setup compiler: $iscc"

return [pscustomobject]@{
    Version  = $Version
    ToolsDir = $toolsDir
    Path     = $iscc
}
