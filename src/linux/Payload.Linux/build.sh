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
# Building
#####################################################################
echo "Building Payload.Linux..."

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

# Directories
THISDIR="$( cd "$(dirname "$0")" ; pwd -P )"
ROOT="$( cd "$THISDIR"/../../.. ; pwd -P )"
SRC="$ROOT/src"
OUT="$ROOT/out"
GCM_SRC="$SRC/shared/Git-Credential-Manager"
PAYLOAD_SRC="$SRC/linux/Payload.Linux"
PAYLOAD_OUT="$OUT/linux/Payload.Linux"

# Build parameters
FRAMEWORK=netcoreapp3.1
RUNTIME=linux-x64

# Perform pre-execution checks
CONFIGURATION="${CONFIGURATION:=Debug}"
if [ -z "$VERSION" ]; then
    die "--version was not set"
fi

PAYLOAD="$PAYLOAD_OUT/tar/$CONFIGURATION/payload"
SYMBOLOUT="$PAYLOAD.sym"
TAROUT="$PAYLOAD_OUT/tar/$CONFIGURATION/gcmcore-linux-x86_64-$VERSION.tar.gz"
DEBOUT="$PAYLOAD_OUT/gcmcore.x86_64.$CONFIGURATION.$VERSION"

# Layout and pack
# Cleanup any old payload directory
if [ -d "$PAYLOAD" ]; then
    echo "Cleaning old payload directory '$PAYLOAD'..."
    rm -rf "$PAYLOAD"
fi

# Ensure payload and symbol directories exists
mkdir -p "$PAYLOAD" "$SYMBOLOUT"

# Publish core application executables
echo "Publishing core application..."
dotnet publish "$GCM_SRC" \
	--configuration="$CONFIGURATION" \
	--framework="$FRAMEWORK" \
	--runtime="$RUNTIME" \
    --self-contained \
    /p:PublishSingleFile \ # maybe also add /p:Version="$VERSION"
	--output="$(make_absolute "$PAYLOAD")" || exit 1

# Collect symbols
echo "Collecting managed symbols..."
mv "$PAYLOAD"/*.pdb "$SYMBOLOUT" || exit 1

echo "Build complete."

#####################################################################
# PACKING
#####################################################################
echo "Packing Payload.Linux..."
# Cleanup any old archive files
if [ -e "$TAROUT" ]; then
    echo "Deleteing old archive '$TAROUT'..."
    rm "$TAROUT"
fi

# Ensure the parent directory for the archive exists
mkdir -p "$(dirname "$TAROUT")"

# Set full read, write, execute permissions for owner and just read and execute permissions for group and other
echo "Setting file permissions..."
/bin/chmod -R 755 "$PAYLOAD" || exit 1

# Build tarball
echo "Building archive..."
cd "$PAYLOAD"
tar -czvf "$TAROUT" * || exit 1

# Build .deb
# TODO

echo "Pack complete."
