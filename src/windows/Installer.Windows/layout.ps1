# Inputs
param ([Parameter(Mandatory)] $CONFIGURATION, [Parameter(Mandatory)] $OUTPUT, $SYMBOLOUTPUT)

Write-Output "Output: $OUTPUT"

# Directories
$THISDIR = $pwd.path
$ROOT = (Get-Item $THISDIR).parent.parent.parent.FullName
$SRC = "$ROOT/src"
$GCM_SRC = "$SRC/shared/Git-Credential-Manager"

# Perform pre-execution checks
$PAYLOAD = "$OUTPUT"
if ($SYMBOLOUTPUT)
{
    $SYMBOLS = "$SYMBOLOUTPUT"
} else {
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
mkdir -p "$PAYLOAD","$SYMBOLS"

# Publish core application executables
Write-Output "Publishing core application..."
dotnet publish "$GCM_SRC" `
	--framework net472 `
	--configuration "$CONFIGURATION" `
	--runtime win-x86 `
	--output "$PAYLOAD"

# Delete libraries that are not needed for Windows but find their way
# into the publish output.
Remove-Item -Path "$PAYLOAD/*.dylib" -Force

# Delete extraneous files that get included for other architectures
# We only care about x86 as the core GCM executable is only targeting x86
Remove-Item -Path "$PAYLOAD/arm/" -Recurse -Force
Remove-Item -Path "$PAYLOAD/arm64/" -Recurse -Force
Remove-Item -Path "$PAYLOAD/x64/" -Recurse -Force
Remove-Item -Path "$PAYLOAD/musl-x64/" -Recurse -Force
Remove-Item -Path "$PAYLOAD/runtimes/win-arm64/" -Recurse -Force
Remove-Item -Path "$PAYLOAD/runtimes/win-x64/" -Recurse -Force

# The Avalonia and MSAL binaries in these directories are already included in
# the $PAYLOAD directory directly, so we can delete these extra copies.
Remove-Item -Path "$PAYLOAD/x86/libSkiaSharp.dll" -Recurse -Force
Remove-Item -Path "$PAYLOAD/x86/libHarfBuzzSharp.dll" -Recurse -Force
Remove-Item -Path "$PAYLOAD/runtimes/win-x86/native/msalruntime_x86.dll" -Recurse -Force

# Delete localized resource assemblies - we don't localize the core GCM assembly anyway
Get-ChildItem "$PAYLOAD" -Recurse -Include "*.resources.dll" | Remove-Item -Force

# Delete any empty directories
Get-ChildItem "$PAYLOAD" -Recurse -Directory `
	| Sort-Object -Property FullName -Descending `
	| Where-Object { ! (Get-ChildItem $_.FullName -File -Recurse).Count } `
	| Remove-Item -Force

# Collect symbols
Write-Output "Collecting managed symbols..."
Move-Item -Path "$PAYLOAD/*.pdb" -Destination "$SYMBOLS"

Write-Output "Layout complete."
