#!/bin/bash
die () {
    echo "$*" >&2
    exit 1
}
make_absolute () {
    case "$1" in
    /*)
        echo "$1"
        ;;
    *)
        echo "$PWD/$1"
        ;;
    esac
}

# Directories
THISDIR="$( cd "$(dirname "$0")" ; pwd -P )"
ROOT="$( cd "$THISDIR"/../../.. ; pwd -P )"
SRC="$ROOT/src"
OUT="$ROOT/out"
INSTALLER_SRC="$SRC/osx/Installer.Mac"
GCM_SRC="$SRC/shared/Git-Credential-Manager"
GCM_UI_SRC="$SRC/shared/Git-Credential-Manager.UI.Avalonia"
BITBUCKET_UI_SRC="$SRC/shared/Atlassian.Bitbucket.UI.Avalonia"
GITHUB_UI_SRC="$SRC/shared/GitHub.UI.Avalonia"
GITLAB_UI_SRC="$SRC/shared/GitLab.UI.Avalonia"

# Build parameters
FRAMEWORK=net6.0

# Parse script arguments
for i in "$@"
do
case "$i" in
    --configuration=*)
    CONFIGURATION="${i#*=}"
    shift # past argument=value
    ;;
    --output=*)
    PAYLOAD="${i#*=}"
    shift # past argument=value
    ;;
    --runtime=*)
    RUNTIME="${i#*=}"
    shift # past argument=value
    ;;
    --symbol-output=*)
    SYMBOLOUT="${i#*=}"
    ;;
    *)
          # unknown option
    ;;
esac
done

# Determine a runtime if one was not provided
if [ -z "$RUNTIME" ]; then
    TEST_RUNTIME=`uname -m`
    case $TEST_RUNTIME in
        "x86_64")
            RUNTIME="osx-x64"
            ;;
        "arm64")
            RUNTIME="osx-arm64"
            ;;
        *)
            die "Unknown runtime '$TEST_RUNTIME'"
            ;;
    esac
fi

echo "Building for runtime '$RUNTIME'"

# Perform pre-execution checks
CONFIGURATION="${CONFIGURATION:=Debug}"
if [ -z "$PAYLOAD" ]; then
	die "--output was not set"
fi
if [ -z "$SYMBOLOUT" ]; then
    SYMBOLOUT="$PAYLOAD.sym"
fi

# Cleanup any old payload directory
if [ -d "$PAYLOAD" ]; then
    echo "Cleaning old payload directory '$PAYLOAD'..."
    rm -rf "$PAYLOAD"
fi

# Ensure payload and symbol directories exists
mkdir -p "$PAYLOAD" "$SYMBOLOUT"

# Copy uninstaller script
echo "Copying uninstall script..."
cp "$INSTALLER_SRC/uninstall.sh" "$PAYLOAD" || exit 1

# Publish core application executables
echo "Publishing core application..."
dotnet publish "$GCM_SRC" \
	--no-restore \
	-m:1 \
	--configuration="$CONFIGURATION" \
	--framework="$FRAMEWORK" \
	--runtime="$RUNTIME" \
	--self-contained \
	--output="$(make_absolute "$PAYLOAD")" || exit 1

echo "Publishing core UI helper..."
dotnet publish "$GCM_UI_SRC" \
	--no-restore \
	-m:1 \
	--configuration="$CONFIGURATION" \
	--framework="$FRAMEWORK" \
	--runtime="$RUNTIME" \
	--self-contained \
	--output="$(make_absolute "$PAYLOAD")" || exit 1

echo "Publishing Bitbucket UI helper..."
dotnet publish "$BITBUCKET_UI_SRC" \
	--no-restore \
	-m:1 \
	--configuration="$CONFIGURATION" \
	--framework="$FRAMEWORK" \
	--runtime="$RUNTIME" \
	--self-contained \
	--output="$(make_absolute "$PAYLOAD")" || exit 1

echo "Publishing GitHub UI helper..."
dotnet publish "$GITHUB_UI_SRC" \
	--no-restore \
	-m:1 \
	--configuration="$CONFIGURATION" \
	--framework="$FRAMEWORK" \
	--runtime="$RUNTIME" \
	--self-contained \
	--output="$(make_absolute "$PAYLOAD")" || exit 1

echo "Publishing GitLab UI helper..."
dotnet publish "$GITLAB_UI_SRC" \
	--no-restore \
	-m:1 \
	--configuration="$CONFIGURATION" \
	--framework="$FRAMEWORK" \
	--runtime="$RUNTIME" \
	--self-contained \
	--output="$(make_absolute "$PAYLOAD")" || exit 1

# Collect symbols
echo "Collecting managed symbols..."
mv "$PAYLOAD"/*.pdb "$SYMBOLOUT" || exit 1

# Remove any unwanted .DS_Store files
echo "Removing unnecessary files..."
find "$PAYLOAD" -name '*.DS_Store' -type f -delete || exit 1

echo "Layout complete."
