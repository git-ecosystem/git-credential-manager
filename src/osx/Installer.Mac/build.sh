#!/bin/bash
die () {
    echo "$*" >&2
    exit 1
}

echo "Building Installer.Mac..."

# Directories
THISDIR="$( cd "$(dirname "$0")" ; pwd -P )"
ROOT="$( cd $THISDIR/../../.. ; pwd -P )"
SRC=$ROOT/src
OUT=$ROOT/out
INSTALLER_SRC=$SRC/osx/Installer.Mac
INSTALLER_OUT=$OUT/osx/Installer.Mac

# Parse script arguments
for i in "$@"
do
case $i in
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
CONFIGURATION=${CONFIGURATION:=Debug}
if [ -z "$VERSION" ]; then
    die "--version was not set"
fi

PAYLOAD=$INSTALLER_OUT/pkg/$CONFIGURATION/payload
PKGOUT=$INSTALLER_OUT/pkg/$CONFIGURATION/gcmcore-osx-$VERSION.pkg

# Layout and pack
$INSTALLER_SRC/layout.sh --configuration=$CONFIGURATION --output=$PAYLOAD || exit 1
$INSTALLER_SRC/pack.sh --payload=$PAYLOAD --version=$VERSION --output=$PKGOUT || exit 1

echo "Build of Installer.Mac complete."
