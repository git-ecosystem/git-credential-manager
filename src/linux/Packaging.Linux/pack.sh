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
PROJ_OUT="$OUT/linux/Packaging.Linux"
INSTALLER_SRC="$SRC/osx/Installer.Mac"

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
    --symbols=*)
    SYMBOLS="${i#*=}"
    shift # past argument=value
    ;;
    --configuration=*)
    CONFIGURATION="${i#*=}"
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
if [ -z "$PAYLOAD" ]; then
    die "--payload was not set"
elif [ ! -d "$PAYLOAD" ]; then
    die "Could not find '$PAYLOAD'. Did you run layout.sh first?"
fi
if [ -z "$SYMBOLS" ]; then
    die "--symbols was not set"
fi

ARCH="`dpkg-architecture -q DEB_HOST_ARCH`"

if test -z "$ARCH"; then
    die "Could not determine host architecture!"
fi

TAROUT="$PROJ_OUT/$CONFIGURATION/tar/"
TARBALL="$TAROUT/gcm-linux_$ARCH.$VERSION.tar.gz"
SYMTARBALL="$TAROUT/gcm-linux_$ARCH.$VERSION-symbols.tar.gz"

DEBOUT="$PROJ_OUT/$CONFIGURATION/deb"
DEBROOT="$DEBOUT/root"
DEBPKG="$DEBOUT/gcm-linux_$ARCH.$VERSION.deb"
mkdir -p "$DEBROOT"

# Set full read, write, execute permissions for owner and just read and execute permissions for group and other
echo "Setting file permissions..."
/bin/chmod -R 755 "$PAYLOAD" || exit 1

echo "Packing Packaging.Linux..."

# Cleanup any old archive files
if [ -e "$TAROUT" ]; then
    echo "Deleteing old archive '$TAROUT'..."
    rm "$TAROUT"
fi

# Ensure the parent directory for the archive exists
mkdir -p "$TAROUT" || exit 1

# Build binaries tarball
echo "Building binaries tarball..."
pushd "$PAYLOAD"
tar -czvf "$TARBALL" * || exit 1
popd

# Build symbols tarball
echo "Building symbols tarball..."
pushd "$SYMBOLS"
tar -czvf "$SYMTARBALL" * || exit 1
popd

# Build .deb
INSTALL_TO="$DEBROOT/usr/local/share/gcm-core/"
LINK_TO="$DEBROOT/usr/local/bin/"
mkdir -p "$DEBROOT/DEBIAN" "$INSTALL_TO" "$LINK_TO" || exit 1

# make the debian control file
# this is purposefully not indented, see
# https://stackoverflow.com/questions/9349616/bash-eof-in-if-statement
# for details
cat >"$DEBROOT/DEBIAN/control" <<EOF
Package: gcm
Version: $VERSION
Section: vcs
Priority: optional
Architecture: $ARCH
Depends:
Maintainer: GCM <gitfundamentals@github.com>
Description: Cross Platform Git Credential Manager command line utility.
 GCM supports authentication with a number of Git hosting providers
 including GitHub, BitBucket, and Azure DevOps.
 For more information see https://aka.ms/gcm
EOF

# Copy all binaries and shared libraries to target installation location
cp -R "$PAYLOAD"/* "$INSTALL_TO" || exit 1

# Create symlink
if [ ! -f "$LINK_TO/git-credential-manager" ]; then
    ln -s -r "$INSTALL_TO/git-credential-manager" \
        "$LINK_TO/git-credential-manager" || exit 1
fi

dpkg-deb -Zxz --root-owner-group --build "$DEBROOT" "$DEBPKG" || exit 1

echo $MESSAGE
