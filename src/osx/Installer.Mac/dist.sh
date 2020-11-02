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
RESXPATH="$INSTALLER_SRC/resources"
DISTPATH="$INSTALLER_SRC/distribution.xml"

# Product information
IDENTIFIER="com.microsoft.gitcredentialmanager.dist"

# Parse script arguments
for i in "$@"
do
case "$i" in
    --version=*)
    VERSION="${i#*=}"
    shift # past argument=value
    ;;
    --package-path=*)
    PACKAGEPATH="${i#*=}"
    shift # past argument=value
    ;;
    --output=*)
    DISTOUT="${i#*=}"
    shift # past argument=value
    ;;
    *)
          # unknown option
    ;;
esac
done

# Perform pre-execution checks
if [ -z "$VERSION" ]; then
    die "--version was not set"
fi
if [ -z "$PACKAGEPATH" ]; then
    die "--package-path was not set"
elif [ ! -d "$PACKAGEPATH" ]; then
    die "Could not find '$PACKAGEPATH'. Did you run pack.sh first?"
fi
if [ -z "$DISTOUT" ]; then
    die "--output was not set"
fi

# Cleanup any old package
if [ -e "$DISTOUT" ]; then
    echo "Deleteing old product package '$DISTOUT'..."
    rm "$DISTOUT"
fi

# Ensure the parent directory for the package exists
mkdir -p "$(dirname "$DISTOUT")"

# Build product installer
echo "Building product package..."
/usr/bin/productbuild \
    --package-path "$PACKAGEPATH" \
    --resources "$RESXPATH" \
    --distribution "$DISTPATH" \
    --identifier "$IDENTIFIER" \
    --version "$VERSION" \
    "$DISTOUT" || exit 1

echo "Product build complete."
