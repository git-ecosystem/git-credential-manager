#!/bin/bash
die () {
    echo "$*" >&2
    exit 1
}

# Directories
THISDIR="$( cd "$(dirname "$0")" ; pwd -P )"
ROOT="$( cd "$THISDIR"/../../.. ; pwd -P )"
SRC="$ROOT/src"
OUT="$ROOT/out"
INSTALLER_SRC="$SRC/osx/Installer.Mac"

# Product information
IDENTIFIER="com.microsoft.gitcredentialmanager"

# Parse script arguments
for i in "$@"
do
case "$i" in
    --version=*)
    VERSION="${i#*=}"
    shift # past argument=value
    ;;
    --payload=*)
    PAYLOAD="${i#*=}"
    shift # past argument=value
    ;;
    --output=*)
    PKGOUT="${i#*=}"
    shift # past argument=value
    ;;
    --runtime=*)
    RUNTIME="${i#*=}"
    shift # past argument=value
    ;;
    *)
          # unknown option
    ;;
esac
done

if [ -z "$RUNTIME" ]; then
    die "--runtime was not set"
fi

case "$RUNTIME" in
    "osx-x64")
        PREFIX="/usr/local"
        ;;
    "osx-arm64")
        PREFIX="/opt/homebrew"
        ;;
    *)
        die "Unknown runtime '$RUNTIME'"
        ;;
esac

INSTALL_LOCATION="$PREFIX/share/gcm-core"

# Perform pre-execution checks
if [ -z "$VERSION" ]; then
    die "--version was not set"
fi
if [ -z "$PAYLOAD" ]; then
    die "--payload was not set"
elif [ ! -d "$PAYLOAD" ]; then
    die "Could not find '$PAYLOAD'. Did you run layout.sh first?"
fi
if [ -z "$PKGOUT" ]; then
    die "--output was not set"
fi

# Cleanup any old component
if [ -e "$PKGOUT" ]; then
    echo "Deleteing old component '$PKGOUT'..."
    rm "$PKGOUT"
fi

# Ensure the parent directory for the component exists
mkdir -p "$(dirname "$PKGOUT")"

# Set full read, write, execute permissions for owner and just read and execute permissions for group and other
echo "Setting file permissions..."
/bin/chmod -R 755 "$PAYLOAD" || exit 1

# Remove any extended attributes (ACEs)
echo "Removing extended attributes..."
/usr/bin/xattr -rc "$PAYLOAD" || exit 1

# Build component packages
echo "Building core component package..."
/usr/bin/pkgbuild \
    --root "$PAYLOAD/" \
    --install-location "$INSTALL_LOCATION" \
    --scripts "$INSTALLER_SRC/scripts" \
    --identifier "$IDENTIFIER" \
    --version "$VERSION" \
    "$PKGOUT" || exit 1

echo "Component pack complete."
