#!/bin/bash
die () {
    echo "$*" >&2
    exit 1
}

echo "Building Payload.Linux..."

# Directories
THISDIR="$( cd "$(dirname "$0")" ; pwd -P )"
ROOT="$( cd "$THISDIR"/../../.. ; pwd -P )"
SRC="$ROOT/src"
OUT="$ROOT/out"
PAYLOAD_SRC="$SRC/linux/Payload.Linux"
PAYLOAD_OUT="$OUT/linux/Payload.Linux"

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

# Perform pre-execution checks
CONFIGURATION="${CONFIGURATION:=Debug}"
if [ -z "$VERSION" ]; then
    die "--version was not set"
fi

PAYLOAD="$PAYLOAD_OUT/tar/$CONFIGURATION/payload"
TAROUT="$PAYLOAD_OUT/tar/$CONFIGURATION/gcmcore-linux-x86_64-$VERSION.tar.gz"

# Layout and pack
"$PAYLOAD_SRC/layout.sh" --configuration="$CONFIGURATION" --output="$PAYLOAD" || exit 1
"$PAYLOAD_SRC/pack.sh" --payload="$PAYLOAD" --output="$TAROUT" || exit 1

echo "Build of Payload.Linux complete."
