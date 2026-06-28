<#
.SYNOPSIS
    Shared helper functions for the Windows build scripts (build/windows).

.DESCRIPTION
    This file is a PowerShell module; import it from a build script one level
    below build/, e.g.:

        Import-Module "$PSScriptRoot/../lib-cli.psm1" -Force

    It is the PowerShell counterpart of build/lib-cli.sh and mirrors the same
    helpers (logging, repo/artifacts paths, runtime/version/configuration
    normalisation), plus Inno Setup compiler resolution. Verbose output uses the
    native PowerShell -Verbose / Write-Verbose mechanism. Only the public helpers
    are exported (see Export-ModuleMember at the end of this file).
#>

Set-StrictMode -Version Latest

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------

# Echo the absolute, resolved path of the repository root: the directory one
# level above this library (build/..). It resolves relative to the library's own
# location, so it returns the same path regardless of the caller's working
# directory or which script dot-sourced it.
function Get-RepoRoot {
    return (Get-Item $PSScriptRoot).Parent.FullName
}

# Internal: build an artifacts directory <root>/out/<kind>/<project>/<pivot>,
# where <pivot> is <configuration>, or <configuration>_<runtime> when a runtime
# is given. The directory is not required to exist.
function Get-ArtifactsDir {
    param(
        [Parameter(Mandatory)] [string] $Kind,
        [Parameter(Mandatory)] [string] $Project,
        [Parameter(Mandatory)] [string] $Configuration,
        [string] $Runtime
    )
    $pivot = if ($Runtime) { "${Configuration}_${Runtime}" } else { $Configuration }
    return (Join-Path (Get-RepoRoot) 'out' $Kind $Project $pivot)
}

# Echo the artifacts build-output directory for project <Project>:
# out/bin/<project>/<config>[_<runtime>], mirroring where 'dotnet build' writes.
function Get-BinDir {
    param(
        [Parameter(Mandatory)] [string] $Project,
        [Parameter(Mandatory)] [string] $Configuration,
        [string] $Runtime
    )
    return (Get-ArtifactsDir -Kind 'bin' -Project $Project -Configuration $Configuration -Runtime $Runtime)
}

# Echo the artifacts publish directory for project <Project>:
# out/publish/<project>/<config>[_<runtime>], mirroring where 'dotnet publish'
# writes.
function Get-PublishDir {
    param(
        [Parameter(Mandatory)] [string] $Project,
        [Parameter(Mandatory)] [string] $Configuration,
        [string] $Runtime
    )
    return (Get-ArtifactsDir -Kind 'publish' -Project $Project -Configuration $Configuration -Runtime $Runtime)
}

# Echo the shared artifacts package directory for build configuration <config>:
# out/package/<config>. Unlike Get-BinDir/Get-PublishDir this is neither
# per-project nor per-runtime: it is the directory that final packages and
# archives are written to, with the runtime encoded in each file name.
function Get-PackageDir {
    param([Parameter(Mandatory)] [string] $Configuration)
    return (Join-Path (Get-RepoRoot) 'out' 'package' $Configuration)
}

# Echo an absolute form of <Path>: an already-absolute path is returned
# unchanged, a relative path is resolved against the current directory. The path
# is not required to exist.
function Get-AbsolutePath {
    param([Parameter(Mandatory)] [string] $Path)
    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }
    return (Join-Path (Get-Location).Path $Path)
}

# ---------------------------------------------------------------------------
# Runtime identifiers
# ---------------------------------------------------------------------------

# Echo a validated Windows runtime identifier. With no -Runtime (or an empty
# one), the host's own runtime is detected from the processor architecture;
# otherwise -Runtime is validated against the supported Windows runtimes. Writes
# a terminating error for an unsupported host or an invalid runtime.
function Resolve-Runtime {
    param([string] $Runtime)
    $valid = @('win-x64', 'win-x86', 'win-arm64')
    if (-not $Runtime) {
        switch ($env:PROCESSOR_ARCHITECTURE) {
            'AMD64' { return 'win-x64' }
            'x86'   { return 'win-x86' }
            'ARM64' { return 'win-arm64' }
            default { Write-Error "unsupported host architecture '$($env:PROCESSOR_ARCHITECTURE)'; specify -Runtime explicitly" -ErrorAction Stop }
        }
    }
    if ($valid -notcontains $Runtime) {
        Write-Error "unknown runtime '$Runtime' (expected one of: $($valid -join ', '))" -ErrorAction Stop
    }
    return $Runtime
}

# ---------------------------------------------------------------------------
# Version
# ---------------------------------------------------------------------------

# Echo the whitespace-trimmed contents of the repository VERSION file (in the
# repo root). Writes a terminating error if the file does not exist.
function Read-VersionFile {
    $file = Join-Path (Get-RepoRoot) 'VERSION'
    if (-not (Test-Path -LiteralPath $file)) {
        Write-Error "version file '$file' not found" -ErrorAction Stop
    }
    return (Get-Content -Raw -LiteralPath $file).Trim()
}

# Echo <Version> as a 3-component 'x.y.z' identifier: extra components are
# dropped and missing ones default to 0. When <Version> is empty, the version is
# read from the repository VERSION file instead.
function Resolve-Version {
    param([string] $Version)
    if (-not $Version) {
        $Version = Read-VersionFile
    }
    $parts = $Version.Split('.')
    $major = if ($parts.Count -ge 1 -and $parts[0]) { $parts[0] } else { '0' }
    $minor = if ($parts.Count -ge 2 -and $parts[1]) { $parts[1] } else { '0' }
    $patch = if ($parts.Count -ge 3 -and $parts[2]) { $parts[2] } else { '0' }
    return "$major.$minor.$patch"
}

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------

# Echo a validated, lower-cased build configuration name: 'debug' or 'release'.
# When <Configuration> is empty, 'release' is returned. Writes a terminating
# error for an unrecognised configuration.
function Resolve-Configuration {
    param([string] $Configuration)
    if (-not $Configuration) {
        return 'release'
    }
    $c = $Configuration.ToLowerInvariant()
    if ($c -ne 'debug' -and $c -ne 'release') {
        Write-Error "unknown configuration '$Configuration' (expected 'Debug' or 'Release')" -ErrorAction Stop
    }
    return $c
}

# ---------------------------------------------------------------------------
# Inno Setup
# ---------------------------------------------------------------------------

# Resolve the path to the Inno Setup command-line compiler (ISCC.exe). When
# -InnoSetup is given it is used directly (and must exist); otherwise 'iscc' is
# looked up on PATH. Writes a terminating error if no compiler can be found.
# MSBuild callers can pass the Tools.InnoSetup package's $(InnoSetupCompiler)
# property; people running the scripts directly need iscc on PATH or must pass
# -InnoSetup.
function Resolve-InnoSetup {
    param([string] $InnoSetup)
    if ($InnoSetup) {
        if (-not (Test-Path -LiteralPath $InnoSetup)) {
            Write-Error "Inno Setup compiler not found at '$InnoSetup'" -ErrorAction Stop
        }
        return (Resolve-Path -LiteralPath $InnoSetup).Path
    }
    $cmd = Get-Command 'iscc' -CommandType Application -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if (-not $cmd) {
        Write-Error "Inno Setup compiler (iscc) not found on PATH; pass -InnoSetup <path> or install Inno Setup" -ErrorAction Stop
    }
    return $cmd.Source
}

# ---------------------------------------------------------------------------
# Exports
# ---------------------------------------------------------------------------

# Get-ArtifactsDir is an internal implementation helper and is intentionally not
# exported; only the public helpers below make up the module's surface.
Export-ModuleMember -Function `
    Get-RepoRoot, Get-BinDir, Get-PublishDir, Get-PackageDir, Get-AbsolutePath, `
    Resolve-Runtime, Read-VersionFile, Resolve-Version, Resolve-Configuration, `
    Resolve-InnoSetup
