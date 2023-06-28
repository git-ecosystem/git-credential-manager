#!/bin/bash
die () {
    echo "$*" >&2
    exit 1
}

echo "Building Installer.Mac..."

# Directories
THISDIR="$( cd "$(dirname "$0")" ; pwd -P )"
ROOT="$( cd "$THISDIR"/../../.. ; pwd -P )"
SRC="$ROOT/src"
OUT="$ROOT/out"
INSTALLER_SRC="$SRC/osx/Installer.Mac"
INSTALLER_OUT="$OUT/osx/Installer.Mac"

# Parse script arguments
for i in "$@"
do
case "$i" in
    --configuration=*)
    CONFIGURATION="${i#*=}"
    shift # past argument=value
    ;;
    --runtime=*)
    RUNTIME="${i#*=}"
    shift
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

OUTDIR="$INSTALLER_OUT/pkg/$CONFIGURATION"
PAYLOAD="$OUTDIR/payload"
COMPONENTDIR="$OUTDIR/components"
COMPONENTOUT="$COMPONENTDIR/com.microsoft.gitcredentialmanager.component.pkg"
DISTOUT="$OUTDIR/gcm-$RUNTIME-$VERSION.pkg"

# Layout and pack
"$INSTALLER_SRC/layout.sh" --configuration="$CONFIGURATION" --output="$PAYLOAD" --runtime="$RUNTIME" || exit 1
"$INSTALLER_SRC/pack.sh" --payload="$PAYLOAD" --version="$VERSION" --output="$COMPONENTOUT" || exit 1
"$INSTALLER_SRC/dist.sh" --package-path="$COMPONENTDIR" --version="$VERSION" --output="$DISTOUT" --runtime="$RUNTIME" || exit 1

echo "Build of Installer.Mac complete."
