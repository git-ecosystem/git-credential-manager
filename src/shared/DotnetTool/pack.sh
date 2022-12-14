#!/bin/bash
die () {
    echo "$*" >&2
    exit 1
}

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
	--publish-dir=*)
    PUBLISH_DIR="${i#*=}"
    shift # past argument=value
    ;;
    *)
          # unknown option
    ;;
esac
done

CONFIGURATION="${CONFIGURATION:=Debug}"
if [ -z "$VERSION" ]; then
    die "--version was not set"
fi

# Directories
THISDIR="$( cd "$(dirname "$0")" ; pwd -P )"
ROOT="$( cd "$THISDIR"/../../.. ; pwd -P )"
SRC="$ROOT/src"
OUT="$ROOT/out"
DOTNET_TOOL="shared/DotnetTool"

if [ -z "$PUBLISH_DIR" ]; then
    PUBLISH_DIR="$OUT/$DOTNET_TOOL/nupkg/$CONFIGURATION"
fi

echo "Creating dotnet tool package..."

dotnet pack "$SRC/$DOTNET_TOOL/DotnetTool.csproj" \
    /p:Configuration="$CONFIGURATION" \
    /p:PackageVersion="$VERSION" \
    /p:PublishDir="$PUBLISH_DIR/"

echo "Dotnet tool pack complete."
