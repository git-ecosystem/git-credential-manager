# Install the MSVC C++ build tools and Windows SDK that Native AOT needs to
# link the git-credential-manager binary on Windows agents.
#
# Publishing with PublishAot=true links the binary with the MSVC linker
# (link.exe) against the Windows SDK user-mode import libraries.
# https://aka.ms/nativeaot-prerequisites
#
# Install VS 2022 Build Tools with the VC.Tools component for the agent's
# architecture (x86.x64 on Intel, ARM64 on ARM) plus a Windows 11 SDK,
# which carries the import libraries for every target architecture.

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('amd64', 'arm64')]
    [string]$Architecture
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

# Always install the x86/x64 VC tools. The x64 tool set includes the VsDevCmd
# VC integration script (Common7\Tools\vsdevcmd\ext\vcvars.bat) that sets
# VCToolsInstallDir, the compiler/linker PATH, and LIB.
# Sadly the ARM64 component does NOT ship that script, so an arm64-only install
# leaves 'vcvarsall' unable to initialise the VC environment correctly.
# Native AOT fails with "Platform linker not found" even though the arm64 linker
# is present on disk.
$components = @('Microsoft.VisualStudio.Component.VC.Tools.x86.x64')

# When targeting arm64, also install the ARM64 component for the arm64 target
# linker and import libraries.
if ($Architecture -eq 'arm64') {
    $components += 'Microsoft.VisualStudio.Component.VC.Tools.ARM64'
}

# The Windows SDK provides the user-mode import libraries (advapi32.lib, etc).
$components += 'Microsoft.VisualStudio.Component.Windows11SDK.22621'

$bootstrapper = "$env:TEMP\vs_BuildTools.exe"
Write-Host "Downloading VS 2022 Build Tools bootstrapper..."
Invoke-WebRequest -Uri 'https://aka.ms/vs/17/release/vs_BuildTools.exe' `
    -OutFile $bootstrapper

$vsArgs = @('--quiet', '--wait', '--norestart', '--nocache')
foreach ($c in $components) { $vsArgs += @('--add', $c) }
Write-Host "Installing VS Build Tools (args: $($vsArgs -join ' '))..."
$start = Get-Date
$p = Start-Process -FilePath $bootstrapper -ArgumentList $vsArgs -Wait -PassThru
$elapsed = (Get-Date) - $start
Write-Host ("Installer exited with code {0} after {1:N0}s" -f `
    $p.ExitCode, $elapsed.TotalSeconds)

Write-Host ""
Write-Host "===== Installer logs in `$env:TEMP ====="
$logs = Get-ChildItem $env:TEMP -Filter 'dd_*.log' -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending
if ($logs) {
    foreach ($log in $logs | Select-Object -First 5) {
        Write-Host "----- $($log.FullName) (last 50 lines) -----"
        Get-Content $log.FullName -Tail 50 -ErrorAction SilentlyContinue
    }
} else {
    Write-Host "(no dd_*.log files found in `$env:TEMP)"
}

# 3010 = reboot required, treated as success.
if ($p.ExitCode -notin 0, 3010) {
    throw "VS Build Tools installer exited with code $($p.ExitCode)"
}

Write-Host ""
Write-Host "===== Confirm components via vswhere ====="
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (Test-Path $vswhere) {
    $installPath = @(& $vswhere -latest -products * -requires $components `
        -property installationPath)[0]
    if ($installPath) {
        Write-Host "Build tools and Windows SDK installed at $installPath"
    } else {
        throw "Required components ($($components -join ', ')) not found after install (see logs above)"
    }
} else {
    Write-Host "vswhere not found at $vswhere"
}

# Native commands above can leave a stray non-zero $LASTEXITCODE behind
# (Windows PowerShell 5.1 tears down piped native commands early); every
# real failure throws, so reaching here means success.
exit 0
