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

# Directories
THISDIR="$( cd "$(dirname "$0")" ; pwd -P )"
ROOT="$( cd "$THISDIR"/../../.. ; pwd -P )"
SRC="$ROOT/src"
OUT="$ROOT/out"
GCM_SRC="$SRC/shared/Git-Credential-Manager"
PROJ_OUT="$OUT/linux/Packaging.Linux"

# Build parameters
FRAMEWORK=net8.0

# Perform pre-execution checks
CONFIGURATION="${CONFIGURATION:=Debug}"
if [ -z "$PAYLOAD" ]; then
    die "--output was not set"
fi
if [ -z "$SYMBOLOUT" ]; then
    SYMBOLOUT="$PAYLOAD.sym"
fi

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

# Ensure directories exists
mkdir -p "$PAYLOAD" "$SYMBOLOUT"

if [ -z "$DOTNET_ROOT" ]; then
    DOTNET_ROOT="$(dirname $(which dotnet))"
fi

# Publish core application executables
echo "Publishing core application..."
if [ -z "$RUNTIME" ]; then
    $DOTNET_ROOT/dotnet publish "$GCM_SRC" \
        --configuration="$CONFIGURATION" \
        --framework="$FRAMEWORK" \
        --self-contained \
        -p:PublishSingleFile=true \
        --output="$(make_absolute "$PAYLOAD")" || exit 1
else
    $DOTNET_ROOT/dotnet publish "$GCM_SRC" \
        --configuration="$CONFIGURATION" \
        --framework="$FRAMEWORK" \
        --runtime="$RUNTIME" \
        --self-contained \
        -p:PublishSingleFile=true \
        --output="$(make_absolute "$PAYLOAD")" || exit 1
fi

# Collect symbols
echo "Collecting managed symbols..."
mv "$PAYLOAD"/*.pdb "$SYMBOLOUT" || exit 1

echo "Build complete."
