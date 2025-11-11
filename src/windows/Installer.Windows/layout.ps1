# Inputs
param ([Parameter(Mandatory)] $Configuration, [Parameter(Mandatory)] $Output, $SymbolOutput)

Write-Output "Output: $Output"

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
	--framework net8.0 `
	--self-contained `
	--configuration "$CONFIGURATION" `
	--runtime win-x86 `
	--output "$PAYLOAD"

# Delete libraries that are not needed for Windows but find their way
# into the publish output.
Remove-Item -Path "$PAYLOAD/*.dylib" -Force -ErrorAction Ignore

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
