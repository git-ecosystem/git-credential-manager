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
PAYLOAD_OUT="$OUT/linux/"

# Build parameters
FRAMEWORK=netcoreapp3.1
RUNTIME=linux-x64

# Perform pre-execution checks
CONFIGURATION="${CONFIGURATION:=Debug}"
if [ -z "$VERSION" ]; then
    die "--version was not set"
fi

ARCH="`dpkg-architecture -q DEB_HOST_ARCH`"
if test -z "$ARCH"; then
  die "Could not determine host architecture!"
fi

# Outputs
PAYLOAD="$PAYLOAD_OUT/payload/$CONFIGURATION"
TAROUT="$PAYLOAD_OUT/gcmcore-linux_$ARCH.$CONFIGURATION.$VERSION.tar.gz"
DEBPKG="$PAYLOAD_OUT/gcmcore-linux/"
DEBOUT="$PAYLOAD_OUT/gcmcore-linux_$ARCH.$CONFIGURATION.$VERSION.deb"
SYMBOLOUT="$PAYLOAD.sym"

# Cleanup payload directory
if [ -d "$PAYLOAD" ]; then
    echo "Cleaning existing payload directory '$PAYLOAD'..."
    rm -rf "$PAYLOAD"
fi

# Ensure directories exists
mkdir -p "$PAYLOAD" "$SYMBOLOUT" "$DEBPKG"

# Publish core application executables
echo "Publishing core application..."
dotnet publish "$GCM_SRC" \
	--configuration="$CONFIGURATION" \
	--framework="$FRAMEWORK" \
	--runtime="$RUNTIME" \
    --self-contained=true \
    "/p:PublishSingleFile=True" \
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
pushd "$PAYLOAD"
tar -czvf "$TAROUT" * || exit 1
popd

# Build .deb
INSTALL_TO="$DEBPKG/usr/bin/"
mkdir -p "$DEBPKG/DEBIAN" "$INSTALL_TO"

# make the debian control file
cat >"$DEBPKG/DEBIAN/control" <<EOF
Package: gcmcore
Version: $VERSION
Section: vcs
Priority: optional
Architecture: $ARCH
Depends:
Maintainer: GCM-Core <gcmcore@microsoft.com>
Description: Cross Platform Git-Credential-Manager-Core command line utility.
 Linux build of the GCM-Core project to support auth with a number of
 git hosting providers including GitHub, BitBucket, and Azure DevOps.
 Hosted at https://github.com/microsoft/Git-Credential-Manager-Core
EOF

# Copy single binary to target installation location
cp "$PAYLOAD/git-credential-manager-core" "$INSTALL_TO"

dpkg-deb --build "$DEBPKG" "$DEBOUT"

echo "Pack complete."
