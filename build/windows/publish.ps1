<#
.SYNOPSIS
    Publishes the Git Credential Manager application for the Windows installer.

.DESCRIPTION
    Publishes git-credential-manager for the target runtime, removes files that
    are not shipped on Windows, and moves debug symbols (.pdb files) out of the
    published output into a sibling symbol directory.

    By default this is a native ahead-of-time (AOT) build; pass -Aot:$false for a
    trimmed, self-contained non-AOT build instead.
#>
[CmdletBinding()]
param (
    [Alias('c')] [string] $Configuration,
    [Alias('r')] [string] $Runtime,
    [Alias('o')] [string] $Output,
    [string] $SymbolOutput,
    [string] $Version,
    [bool] $Aot = $true
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'

Import-Module "$PSScriptRoot/../lib-cli.psm1" -Force

# Normalise arguments / apply defaults.
$Configuration = Resolve-Configuration $Configuration
$Runtime = Resolve-Runtime $Runtime
$Version = Resolve-Version $Version

# Directories
$GcmSrc = Join-Path (Get-RepoRoot) 'src' 'git-credential-manager'

# Resolve the publish output directory (default: the project's artifacts publish
# directory) and the sibling directory that debug symbols are separated into.
if ($Output) {
    $OutDir = Get-AbsolutePath $Output
} else {
    $OutDir = Get-PublishDir -Project 'git-credential-manager' -Configuration $Configuration -Runtime $Runtime
}
if ($SymbolOutput) {
    $SymOutDir = Get-AbsolutePath $SymbolOutput
} else {
    $SymOutDir = "$OutDir.sym"
}
if ($SymOutDir -eq $OutDir) {
    Write-Error "-SymbolOutput must differ from the publish output directory"
}

Write-Verbose "configuration: $Configuration"
Write-Verbose "runtime:       $Runtime"
Write-Verbose "version:       $Version"
Write-Verbose "output dir:    $OutDir"
Write-Verbose "symbol dir:    $SymOutDir"

# Clean any existing output and symbol directories.
foreach ($dir in @($OutDir, $SymOutDir)) {
    if (Test-Path -LiteralPath $dir) {
        Write-Verbose "Cleaning existing directory '$dir'..."
        Remove-Item -Recurse -Force -LiteralPath $dir
    }
}
New-Item -ItemType Directory -Path $OutDir, $SymOutDir -Force | Out-Null

# By default the application is published ahead-of-time (AOT) compiled, as
# configured in the project. For a non-AOT build, just turn AOT off: the
# project then enables trimming, which implies a self-contained publish, so
# --self-contained does not need to be passed explicitly.
$aotArgs = @()
if (-not $Aot) {
    $aotArgs = @('-p:PublishAot=false')
}

# Publish the application to the resolved output directory.
Write-Information "Publishing application..."
dotnet publish "$GcmSrc" `
    -v:normal `
    --configuration $Configuration `
    --runtime $Runtime `
    --output $OutDir `
    -p:VersionOverride=$Version `
    @aotArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to publish application (dotnet publish exited $LASTEXITCODE)"
}

# Remove files that are published but not shipped on Windows: non-Windows native
# libraries, and the native libraries for the runtimes we are not building.
Write-Information "Removing files not shipped on Windows..."
Remove-Item -Path "$OutDir/*.dylib" -Force -ErrorAction Ignore
Remove-Item -Path "$OutDir/musl-x64/" -Recurse -Force -ErrorAction Ignore

switch ($Runtime) {
    'win-x86' {
        Remove-Item -Path "$OutDir/arm/", "$OutDir/arm64/", "$OutDir/x64/" -Recurse -Force -ErrorAction Ignore
        Remove-Item -Path "$OutDir/runtimes/win-arm64/", "$OutDir/runtimes/win-x64/" -Recurse -Force -ErrorAction Ignore
        # The Avalonia and MSAL binaries are already included in $OutDir directly.
        Remove-Item -Path "$OutDir/x86/libSkiaSharp.dll", "$OutDir/x86/libHarfBuzzSharp.dll" -Force -ErrorAction Ignore
        Remove-Item -Path "$OutDir/runtimes/win-x86/native/msalruntime_x86.dll" -Force -ErrorAction Ignore
    }
    'win-x64' {
        Remove-Item -Path "$OutDir/arm/", "$OutDir/arm64/", "$OutDir/x86/" -Recurse -Force -ErrorAction Ignore
        Remove-Item -Path "$OutDir/runtimes/win-arm64/", "$OutDir/runtimes/win-x86/" -Recurse -Force -ErrorAction Ignore
        # The Avalonia and MSAL binaries are already included in $OutDir directly.
        Remove-Item -Path "$OutDir/x64/libSkiaSharp.dll", "$OutDir/x64/libHarfBuzzSharp.dll" -Force -ErrorAction Ignore
        Remove-Item -Path "$OutDir/x64/libSkiaSharp.so", "$OutDir/x64/libHarfBuzzSharp.so" -Force -ErrorAction Ignore
        Remove-Item -Path "$OutDir/runtimes/win-x64/native/msalruntime.dll" -Force -ErrorAction Ignore
    }
    'win-arm64' {
        Remove-Item -Path "$OutDir/arm/", "$OutDir/x86/", "$OutDir/x64/" -Recurse -Force -ErrorAction Ignore
        Remove-Item -Path "$OutDir/runtimes/win-x86/", "$OutDir/runtimes/win-x64/" -Recurse -Force -ErrorAction Ignore
        # The Avalonia and MSAL binaries are already included in $OutDir directly.
        Remove-Item -Path "$OutDir/arm64/libSkiaSharp.dll", "$OutDir/arm64/libHarfBuzzSharp.dll" -Force -ErrorAction Ignore
        Remove-Item -Path "$OutDir/arm64/libSkiaSharp.so", "$OutDir/arm64/libHarfBuzzSharp.so" -Force -ErrorAction Ignore
        Remove-Item -Path "$OutDir/runtimes/win-arm64/native/msalruntime_arm64.dll" -Force -ErrorAction Ignore
    }
}

# Remove localized resource assemblies (the core GCM assembly is not localized).
Get-ChildItem -Path $OutDir -Recurse -Include '*.resources.dll' |
    Remove-Item -Force -ErrorAction Ignore

# Remove any now-empty directories.
Get-ChildItem -Path $OutDir -Recurse -Directory |
    Sort-Object -Property FullName -Descending |
    Where-Object { -not (Get-ChildItem -LiteralPath $_.FullName -File -Recurse) } |
    Remove-Item -Force -ErrorAction Ignore

# Separate debug symbols out of the shipping payload into the sibling symbol
# directory, so the published output holds only the files we ship.
Write-Information "Separating debug symbols into '$SymOutDir'..."
$pdbs = Get-ChildItem -Path $OutDir -Filter '*.pdb' -File -ErrorAction Ignore
if ($pdbs) {
    $pdbs | Move-Item -Destination $SymOutDir -Force
}

Write-Information "Publish complete."
