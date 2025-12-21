<#
.SYNOPSIS
    Lays out the .NET tool package directory.

.PARAMETER Configuration
    Build configuration (Debug/Release). Defaults to Debug.

.PARAMETER Output
    Root output directory for the nupkg layout. If omitted:
    out/shared/DotnetTool/nupkg/<Configuration>

.EXAMPLE
    pwsh ./layout.ps1 -Configuration Release

.EXAMPLE
    pwsh ./layout.ps1 -Output C:\temp\tool-layout

#>

[CmdletBinding()]
param(
	[string]$Configuration = "Debug",
	[string]$Output
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Make-Absolute {
	param([string]$Path)
	if ([string]::IsNullOrWhiteSpace($Path)) { return $null }
	if ([System.IO.Path]::IsPathRooted($Path)) { return $Path }
	return (Join-Path -Path (Get-Location) -ChildPath $Path)
}

Write-Host "Starting layout..." -ForegroundColor Cyan

# Directories
$ScriptDir = $PSScriptRoot
$Root = (Resolve-Path (Join-Path $ScriptDir "..\..\..")).Path
$Src = Join-Path $Root "src"
$Out = Join-Path $Root "out"
$DotnetToolRel = "shared/DotnetTool"
$GcmSrc = Join-Path $Src "shared\Git-Credential-Manager"
$ProjOut = Join-Path $Out $DotnetToolRel

$Framework = "net8.0"

if (-not $Output -or $Output.Trim() -eq "") {
	$Output = Join-Path $ProjOut "nupkg\$Configuration"
}

$ImgOut = Join-Path $Output "images"
$BinOut = Join-Path $Output "tools\$Framework\any"

# Cleanup previous layout
if (Test-Path $Output) {
	Write-Host "Cleaning existing output directory '$Output'..."
	Remove-Item -Force -Recurse $Output
}

# Recreate directories
$null = New-Item -ItemType Directory -Path $BinOut -Force
$null = New-Item -ItemType Directory -Path $ImgOut -Force

# Determine DOTNET_ROOT if not set
if (-not $env:DOTNET_ROOT -or $env:DOTNET_ROOT.Trim() -eq "") {
	$dotnetCmd = Get-Command dotnet -ErrorAction Stop
	$env:DOTNET_ROOT = Split-Path -Parent $dotnetCmd.Source
}

Write-Host "Publishing core application..."
& "$env:DOTNET_ROOT/dotnet" publish $GcmSrc `
	--configuration $Configuration `
	--framework $Framework `
	--output (Make-Absolute $BinOut) `
	-p:UseAppHost=false

if ($LASTEXITCODE -ne 0) {
	Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
	exit $LASTEXITCODE
}

Write-Host "Copying package configuration file..."
Copy-Item -Path (Join-Path $Src "$DotnetToolRel\DotnetToolSettings.xml") -Destination $BinOut -Force

Write-Host "Copying images..."
Copy-Item -Path (Join-Path $Src "$DotnetToolRel\icon.png") -Destination $ImgOut -Force

Write-Host "Layout complete." -ForegroundColor Green
