# Inputs
param ([Parameter(Mandatory)] $Configuration, [Parameter(Mandatory)] $Output, $RuntimeIdentifier, $SymbolOutput)

# Trim trailing slashes from output paths
$Output = $Output.TrimEnd('\','/')
$SymbolOutput = $SymbolOutput.TrimEnd('\','/')

Write-Output "Output: $Output"

# Determine a runtime if one was not provided
if (-not $RuntimeIdentifier) {
    $arch = $env:PROCESSOR_ARCHITECTURE
    switch ($arch) {
        "AMD64" { $RuntimeIdentifier = "win-x64" }
        "x86"   { $RuntimeIdentifier = "win-x86" }
        "ARM64" { $RuntimeIdentifier = "win-arm64" }
        default {
            Write-Host "Unknown architecture: $arch"
            exit 1
        }
    }
}

Write-Output "Building for runtime '$RuntimeIdentifier'"

if ($RuntimeIdentifier -ne 'win-x86' -and $RuntimeIdentifier -ne 'win-x64' -and $RuntimeIdentifier -ne 'win-arm64') {
    Write-Host "Unsupported RuntimeIdentifier: $RuntimeIdentifier"
    exit 1
}

# Directories
$THISDIR = $PSScriptRoot
$ROOT = (Get-Item $THISDIR).Parent.Parent.Parent.FullName
$SRC = "$ROOT\src"
$GCM_SRC = "$SRC\shared\Git-Credential-Manager"

# Perform pre-execution checks
$PAYLOAD = "$Output"
if ($SymbolOutput)
{
    $SYMBOLS = "$SymbolOutput"
}
else
{
    $SYMBOLS = "$PAYLOAD.sym"
}

# Clean up any old payload and symbols directories
if (Test-Path -Path $PAYLOAD)
{
    Write-Output "Cleaning old payload directory '$PAYLOAD'..."
    Remove-Item -Recurse "$PAYLOAD" -Force
}

if (Test-Path -Path $SYMBOLS)
{
    Write-Output "Cleaning old symbols directory '$SYMBOLS'..."
    Remove-Item -Recurse "$SYMBOLS" -Force
}

# Ensure payload and symbol directories exist
mkdir -p "$PAYLOAD","$SYMBOLS" | Out-Null

# Publish core application executables
Write-Output "Publishing core application..."
dotnet publish "$GCM_SRC" `
	--framework net472 `
	--configuration "$Configuration" `
	--runtime $RuntimeIdentifier `
	--output "$PAYLOAD"

# Delete libraries that are not needed for Windows but find their way
# into the publish output.
Remove-Item -Path "$PAYLOAD/*.dylib" -Force -ErrorAction Ignore

# Delete extraneous files that get included for other runtimes
Remove-Item -Path "$PAYLOAD/musl-x64/" -Recurse -Force -ErrorAction Ignore

switch ($RuntimeIdentifier) {
    "win-x86" {
        Remove-Item -Path "$PAYLOAD/arm/" -Recurse -Force -ErrorAction Ignore
        Remove-Item -Path "$PAYLOAD/arm64/" -Recurse -Force -ErrorAction Ignore
        Remove-Item -Path "$PAYLOAD/x64/" -Recurse -Force -ErrorAction Ignore
        Remove-Item -Path "$PAYLOAD/runtimes/win-arm64/" -Recurse -Force -ErrorAction Ignore
        Remove-Item -Path "$PAYLOAD/runtimes/win-x64/" -Recurse -Force -ErrorAction Ignore
        # The Avalonia and MSAL binaries are already included in the $PAYLOAD directory directly
        Remove-Item -Path "$PAYLOAD/x86/libSkiaSharp.dll" -Force -ErrorAction Ignore
        Remove-Item -Path "$PAYLOAD/x86/libHarfBuzzSharp.dll" -Force -ErrorAction Ignore
        Remove-Item -Path "$PAYLOAD/runtimes/win-x86/native/msalruntime_x86.dll" -Force -ErrorAction Ignore
    }
    "win-x64" {
        Remove-Item -Path "$PAYLOAD/arm/" -Recurse -Force -ErrorAction Ignore
        Remove-Item -Path "$PAYLOAD/arm64/" -Recurse -Force -ErrorAction Ignore
        Remove-Item -Path "$PAYLOAD/x86/" -Recurse -Force -ErrorAction Ignore
        Remove-Item -Path "$PAYLOAD/runtimes/win-arm64/" -Recurse -Force -ErrorAction Ignore
        Remove-Item -Path "$PAYLOAD/runtimes/win-x86/" -Recurse -Force -ErrorAction Ignore
        # The Avalonia and MSAL binaries are already included in the $PAYLOAD directory directly
        Remove-Item -Path "$PAYLOAD/x64/libSkiaSharp.dll" -Force -ErrorAction Ignore
        Remove-Item -Path "$PAYLOAD/x64/libHarfBuzzSharp.dll" -Force -ErrorAction Ignore
        Remove-Item -Path "$PAYLOAD/x64/libSkiaSharp.so" -Force -ErrorAction Ignore
        Remove-Item -Path "$PAYLOAD/x64/libHarfBuzzSharp.so" -Force -ErrorAction Ignore
        Remove-Item -Path "$PAYLOAD/runtimes/win-x64/native/msalruntime.dll" -Force -ErrorAction Ignore
    }
    "win-arm64" {
        Remove-Item -Path "$PAYLOAD/arm/" -Recurse -Force -ErrorAction Ignore
        Remove-Item -Path "$PAYLOAD/x86/" -Recurse -Force -ErrorAction Ignore
        Remove-Item -Path "$PAYLOAD/x64/" -Recurse -Force -ErrorAction Ignore
        Remove-Item -Path "$PAYLOAD/runtimes/win-x86/" -Recurse -Force -ErrorAction Ignore
        Remove-Item -Path "$PAYLOAD/runtimes/win-x64/" -Recurse -Force -ErrorAction Ignore
        # The Avalonia and MSAL binaries are already included in the $PAYLOAD directory directly
        Remove-Item -Path "$PAYLOAD/arm64/libSkiaSharp.dll" -Force -ErrorAction Ignore
        Remove-Item -Path "$PAYLOAD/arm64/libHarfBuzzSharp.dll" -Force -ErrorAction Ignore
        Remove-Item -Path "$PAYLOAD/arm64/libSkiaSharp.so" -Force -ErrorAction Ignore
        Remove-Item -Path "$PAYLOAD/arm64/libHarfBuzzSharp.so" -Force -ErrorAction Ignore
        Remove-Item -Path "$PAYLOAD/runtimes/win-arm64/native/msalruntime_arm64.dll" -Force -ErrorAction Ignore
    }
}

# Delete localized resource assemblies - we don't localize the core GCM assembly anyway
Get-ChildItem "$PAYLOAD" -Recurse -Include "*.resources.dll" | Remove-Item -Force -ErrorAction Ignore

# Delete any empty directories
Get-ChildItem "$PAYLOAD" -Recurse -Directory `
	| Sort-Object -Property FullName -Descending `
	| Where-Object { ! (Get-ChildItem $_.FullName -File -Recurse).Count } `
	| Remove-Item -Force

# Collect symbols
Write-Output "Collecting managed symbols..."
Move-Item -Path "$PAYLOAD/*.pdb" -Destination "$SYMBOLS"

Write-Output "Layout complete."
