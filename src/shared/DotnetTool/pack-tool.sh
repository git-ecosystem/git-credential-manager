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

#####################################################################
# Lay out
#####################################################################
echo "Laying out files for dotnet tool..."
# Parse script arguments
for i in "$@"
do
case "$i" in
    --configuration=*)
    CONFIGURATION="${i#*=}"
    shift # past argument=value
    ;;
    --version=*)
    VERSION="${i#*=}"
    shift # past argument=value
    ;;
    *)
          # unknown option
    ;;
esac
done

if [ -z "$VERSION" ]; then
    VERSION="$GitBuildVersionSimple"
fi

# Directories
THISDIR="$( cd "$(dirname "$0")" ; pwd -P )"
ROOT="$( cd "$THISDIR"/../../.. ; pwd -P )"
SRC="$ROOT/src"
OUT="$ROOT/out"
GCM_SRC="$SRC/shared/Git-Credential-Manager"
BITBUCKET_UI_SRC="$SRC/shared/Atlassian.Bitbucket.UI.Avalonia"
GITHUB_UI_SRC="$SRC/shared/GitHub.UI.Avalonia"
GITLAB_UI_SRC="$SRC/shared/GitLab.UI.Avalonia"
DOTNET_TOOL="shared/DotnetTool"
PROJ_OUT="$OUT/$DOTNET_TOOL"

PACKAGE="$ROOT/nuget"
CONFIGURATION="${CONFIGURATION:=Release}"

# Build parameters
FRAMEWORK=net6.0

# Outputs
PAYLOAD="$PROJ_OUT/payload/$CONFIGURATION"
SYMBOLOUT="$PROJ_OUT/payload.sym/$CONFIGURATION"

# Cleanup payload directory
if [ -d "$PAYLOAD" ]; then
    echo "Cleaning existing payload directory '$PAYLOAD'..."
    rm -rf "$PAYLOAD"
fi

# Cleanup symbol directory
if [ -d "$SYMBOLOUT" ]; then
    echo "Cleaning existing symbols directory '$SYMBOLOUT'..."
    rm -rf "$SYMBOLOUT"
fi

# Cleanup package directory
if [ -d "$PACKAGE" ]; then
    echo "Cleaning existing package directory '$PACKAGE'..."
    rm -rf "$PACKAGE"
fi

# Ensure directories exist
mkdir -p "$PAYLOAD" "$SYMBOLOUT" "$PACKAGE"

if [ -z "$DOTNET_ROOT" ]; then
    DOTNET_ROOT="$(dirname $(which dotnet))"
fi

# Publish core application executables
echo "Publishing core application..."
$DOTNET_ROOT/dotnet publish "$GCM_SRC" \
	--configuration="$CONFIGURATION" \
	--framework="$FRAMEWORK" \
	--output="$(make_absolute "$PAYLOAD")" \
    -p:UseAppHost=false || exit 1

echo "Publishing Bitbucket UI helper..."
$DOTNET_ROOT/dotnet publish "$BITBUCKET_UI_SRC" \
	--configuration="$CONFIGURATION" \
	--framework="$FRAMEWORK" \
	--output="$(make_absolute "$PAYLOAD")" \
    -p:UseAppHost=false || exit 1

echo "Publishing GitHub UI helper..."
$DOTNET_ROOT/dotnet publish "$GITHUB_UI_SRC" \
	--configuration="$CONFIGURATION" \
	--framework="$FRAMEWORK" \
	--output="$(make_absolute "$PAYLOAD")" \
    -p:UseAppHost=false || exit 1

echo "Publishing GitLab UI helper..."
$DOTNET_ROOT/dotnet publish "$GITLAB_UI_SRC" \
	--configuration="$CONFIGURATION" \
	--framework="$FRAMEWORK" \
	--output="$(make_absolute "$PAYLOAD")" \
    -p:UseAppHost=false || exit 1

# Collect symbols
echo "Collecting managed symbols..."
mv "$PAYLOAD"/*.pdb "$SYMBOLOUT" || exit 1

echo "Build complete."

#####################################################################
# Pack dotnet tool
#####################################################################
echo "Creating dotnet tool package..."

mkdir -p "$PACKAGE" || exit 1
echo "Laying out files..."
cp -r "$SRC/$DOTNET_TOOL/DotnetToolSettings.xml" \
    "$PAYLOAD/." \
    "$PACKAGE/"

dotnet pack "$SRC/$DOTNET_TOOL/DotnetTool.csproj" /p:PackageVersion="$VERSION" /p:PublishDir="$PACKAGE/"

echo "Dotnet tool pack complete."
