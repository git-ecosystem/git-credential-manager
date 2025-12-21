<#
.SYNOPSIS
    Creates the NuGet package for the .NET tool.

.PARAMETER Configuration
    Build configuration (Debug/Release). Defaults to Debug.

.PARAMETER Version
    Package version (required).

.PARAMETER PackageRoot
    Root of the pre-laid-out package structure (from layout). Defaults to:
    out/shared/DotnetTool/nupkg/<Configuration>

.PARAMETER Output
    Optional directory for the produced .nupkg/.snupkg. If omitted NuGet chooses.

.EXAMPLE
    pwsh ./pack.ps1 -Version 2.0.123-beta

.EXAMPLE
    pwsh ./pack.ps1 -Configuration Release -Version 2.1.0 -Output C:\pkgs

#>

[CmdletBinding()]
param(
	[string]$Configuration = "Debug",
	[Parameter(Mandatory = $true)]
	[string]$Version,
	[string]$PackageRoot,
	[string]$Output
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Host "Starting pack..." -ForegroundColor Cyan

# Directories
$ScriptDir = $PSScriptRoot
$Root = (Resolve-Path (Join-Path $ScriptDir "..\..\..")).Path
$Src = Join-Path $Root "src"
$Out = Join-Path $Root "out"
$DotnetToolRel = "shared\DotnetTool"
$NuspecFile = Join-Path $Src "$DotnetToolRel\dotnet-tool.nuspec"

if (-not (Test-Path $NuspecFile)) {
	Write-Error "Could not locate nuspec file at '$NuspecFile'"
	exit 1
}

if (-not $PackageRoot -or $PackageRoot.Trim() -eq "") {
	$PackageRoot = Join-Path $Out "$DotnetToolRel\nupkg\$Configuration"
}

if (-not (Test-Path $PackageRoot)) {
	Write-Error "Package root '$PackageRoot' does not exist. Run layout.ps1 first."
	exit 1
}

# Locate nuget
$nugetCmd = Get-Command nuget -ErrorAction SilentlyContinue
if (-not $nugetCmd) {
	Write-Error "nuget CLI not found in PATH (install: https://www.nuget.org/downloads)"
	exit 1
}
$nugetExe = $nugetCmd.Source

Write-Host "Creating .NET tool package..."

$packArgs = @(
	"pack", "$NuspecFile",
	"-Properties", "Configuration=$Configuration",
	"-Version", $Version,
	"-Symbols", "-SymbolPackageFormat", "snupkg",
	"-BasePath", "$PackageRoot"
)

if ($Output -and $Output.Trim() -ne "") {
	if (-not (Test-Path $Output)) {
		Write-Host "Creating output directory '$Output'..."
		New-Item -ItemType Directory -Force -Path $Output | Out-Null
	}
	$packArgs += @("-OutputDirectory", "$Output")
}

& $nugetExe @packArgs

if ($LASTEXITCODE -ne 0) {
	Write-Error "nuget pack failed with exit code $LASTEXITCODE"
	exit $LASTEXITCODE
}

Write-Host ".NET tool pack complete." -ForegroundColor Green
